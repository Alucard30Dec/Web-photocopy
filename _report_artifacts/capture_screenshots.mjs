import { spawn } from "node:child_process";
import { mkdir, writeFile, rm } from "node:fs/promises";
import path from "node:path";

const chromePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const baseUrl = "http://localhost:5250";
const outDir = "E:\\OneDrive - 0dpmr\\WebPhotocopy\\_report_artifacts\\screenshots";
const userDataDir = "E:\\OneDrive - 0dpmr\\WebPhotocopy\\_report_artifacts\\chrome-profile";
const remotePort = 9333;
const viewport = { width: 1365, height: 900, deviceScaleFactor: 1 };

await mkdir(outDir, { recursive: true });
await rm(userDataDir, { recursive: true, force: true });

const chrome = spawn(chromePath, [
  `--remote-debugging-port=${remotePort}`,
  `--user-data-dir=${userDataDir}`,
  "--headless=new",
  "--disable-gpu",
  "--no-first-run",
  "--disable-extensions",
  "--ignore-certificate-errors",
  "about:blank"
], { stdio: "ignore", windowsHide: true });

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function fetchJson(url, options = {}) {
  const response = await fetch(url, options);
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText} for ${url}`);
  }
  return response.json();
}

async function waitForChrome() {
  for (let i = 0; i < 80; i += 1) {
    try {
      return await fetchJson(`http://127.0.0.1:${remotePort}/json/version`);
    } catch {
      await sleep(250);
    }
  }
  throw new Error("Chrome DevTools did not become available.");
}

class CdpPage {
  constructor(wsUrl) {
    this.ws = new WebSocket(wsUrl);
    this.nextId = 1;
    this.pending = new Map();
    this.events = new Map();
    this.ready = new Promise((resolve, reject) => {
      this.ws.addEventListener("open", resolve, { once: true });
      this.ws.addEventListener("error", reject, { once: true });
    });
    this.ws.addEventListener("message", event => {
      const message = JSON.parse(event.data);
      if (message.id && this.pending.has(message.id)) {
        const { resolve, reject } = this.pending.get(message.id);
        this.pending.delete(message.id);
        if (message.error) {
          reject(new Error(message.error.message));
        } else {
          resolve(message.result);
        }
        return;
      }
      if (message.method && this.events.has(message.method)) {
        for (const resolve of this.events.get(message.method)) {
          resolve(message.params || {});
        }
        this.events.delete(message.method);
      }
    });
  }

  async send(method, params = {}) {
    await this.ready;
    const id = this.nextId++;
    const payload = JSON.stringify({ id, method, params });
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.ws.send(payload);
    });
  }

  once(method, timeoutMs = 15000) {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => reject(new Error(`Timed out waiting for ${method}`)), timeoutMs);
      const wrapped = params => {
        clearTimeout(timer);
        resolve(params);
      };
      if (!this.events.has(method)) {
        this.events.set(method, []);
      }
      this.events.get(method).push(wrapped);
    });
  }

  async init() {
    await this.send("Page.enable");
    await this.send("Runtime.enable");
    await this.send("Network.enable");
    await this.send("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: viewport.deviceScaleFactor,
      mobile: false
    });
  }

  async eval(expression) {
    const result = await this.send("Runtime.evaluate", {
      expression,
      awaitPromise: true,
      returnByValue: true
    });
    if (result.exceptionDetails) {
      throw new Error(result.exceptionDetails.text || "Runtime.evaluate failed.");
    }
    return result.result?.value;
  }

  async waitReady(extra = "true", timeoutMs = 20000) {
    const started = Date.now();
    while (Date.now() - started < timeoutMs) {
      const ok = await this.eval(`document.readyState === "complete" && (${extra})`);
      if (ok) return;
      await sleep(250);
    }
    throw new Error("Page did not become ready.");
  }

  async goto(relativeOrAbsolute) {
    const url = relativeOrAbsolute.startsWith("http")
      ? relativeOrAbsolute
      : `${baseUrl}${relativeOrAbsolute}`;
    const load = this.once("Page.loadEventFired", 20000).catch(() => null);
    await this.send("Page.navigate", { url });
    await load;
    await this.waitReady();
  }

  async clearCookies() {
    await this.send("Network.clearBrowserCookies");
  }

  async screenshot(fileName, fullPage = false) {
    if (fullPage) {
      const metrics = await this.send("Page.getLayoutMetrics");
      const height = Math.min(Math.ceil(metrics.contentSize.height), 2200);
      await this.send("Emulation.setDeviceMetricsOverride", {
        width: viewport.width,
        height,
        deviceScaleFactor: viewport.deviceScaleFactor,
        mobile: false
      });
    } else {
      await this.send("Emulation.setDeviceMetricsOverride", {
        width: viewport.width,
        height: viewport.height,
        deviceScaleFactor: viewport.deviceScaleFactor,
        mobile: false
      });
    }
    await sleep(350);
    const result = await this.send("Page.captureScreenshot", {
      format: "png",
      fromSurface: true,
      captureBeyondViewport: fullPage
    });
    await writeFile(path.join(outDir, fileName), Buffer.from(result.data, "base64"));
  }
}

async function createPage() {
  await waitForChrome();
  const target = await fetchJson(`http://127.0.0.1:${remotePort}/json/new?about:blank`, { method: "PUT" });
  const page = new CdpPage(target.webSocketDebuggerUrl);
  await page.init();
  return page;
}

async function login(page, loginPath, email, password) {
  await page.clearCookies();
  await page.goto(loginPath);
  const loginPathLower = loginPath.toLowerCase();
  await page.eval(`
    (() => {
      const email = document.querySelector('input[name="Email"], input[type="email"]');
      const password = document.querySelector('input[name="Password"], input[type="password"]');
      if (!email || !password) throw new Error('Login fields not found');
      email.value = ${JSON.stringify(email)};
      email.dispatchEvent(new Event('input', { bubbles: true }));
      password.value = ${JSON.stringify(password)};
      password.dispatchEvent(new Event('input', { bubbles: true }));
      const form = password.closest('form') || document.querySelector('form');
      form.requestSubmit ? form.requestSubmit() : form.submit();
      return true;
    })()
  `);
  await page.waitReady(`location.pathname.toLowerCase() !== ${JSON.stringify(loginPathLower)}`, 12000);
}

async function loginAny(page, loginPath, credentials, debugPrefix) {
  let lastError = null;
  for (const credential of credentials) {
    try {
      await login(page, loginPath, credential.email, credential.password);
      return credential.email;
    } catch (error) {
      lastError = error;
      await page.screenshot(`${debugPrefix}-failed-${credential.email.replace(/[^a-z0-9]+/gi, "-")}.png`, false);
    }
  }
  throw lastError || new Error(`Unable to login at ${loginPath}`);
}

async function main() {
  const page = await createPage();

  const publicShots = [
    ["01-public-home.png", "/Home", true],
    ["02-public-shops.png", "/Shops", true],
    ["03-public-branch.png", "/ToanPhotocopy", true],
    ["04-customer-login.png", "/ToanPhotocopy/Login", false],
  ];
  for (const [file, url, full] of publicShots) {
    await page.goto(url);
    await page.screenshot(file, full);
  }

  await loginAny(page, "/ToanPhotocopy/Login", [
    { email: "sinhvien01@webphotocopyhub.local", password: "Student@123" },
    { email: "sinhvien01@photocopyhub.local", password: "Student@123" },
  ], "customer-login");
  const customerShots = [
    ["05-customer-dashboard.png", "/ToanPhotocopy/Dashboard", true],
    ["06-customer-print-create.png", "/ToanPhotocopy/PrintJobs/Create", true],
    ["07-customer-print-list.png", "/ToanPhotocopy/PrintJobs", true],
    ["08-customer-wallet.png", "/ToanPhotocopy/Wallet", true],
    ["09-customer-topup.png", "/ToanPhotocopy/Wallet/TopUp", true],
    ["10-customer-products.png", "/ToanPhotocopy/Products", true],
    ["11-customer-support-create.png", "/ToanPhotocopy/SupportOrders/Create", true],
  ];
  for (const [file, url, full] of customerShots) {
    await page.goto(url);
    await page.screenshot(file, full);
  }

  await loginAny(page, "/ToanPhotocopy/Admin/Login", [
    { email: "operator@webphotocopyhub.local", password: "Operator@123456" },
    { email: "operator@photocopyhub.local", password: "Operator@123456" },
  ], "shop-login");
  const shopShots = [
    ["12-shop-dashboard.png", "/ToanPhotocopy/Admin", true],
    ["13-shop-print-queue.png", "/ToanPhotocopy/Admin/PrintJobs", true],
    ["14-shop-topups.png", "/ToanPhotocopy/Admin/TopUpRequests", true],
    ["15-shop-counter-topup.png", "/ToanPhotocopy/Admin/TopUpRequests/CounterTopUp", true],
    ["16-shop-inventory.png", "/ToanPhotocopy/Admin/Inventory", true],
  ];
  for (const [file, url, full] of shopShots) {
    await page.goto(url);
    await page.screenshot(file, full);
  }

  await loginAny(page, "/Admin/Login", [
    { email: "admin@webphotocopyhub.local", password: "Admin@123456" },
    { email: "admin@photocopyhub.local", password: "Admin@123456" },
  ], "admin-login");
  const adminShots = [
    ["17-admin-dashboard.png", "/Admin", true],
    ["18-admin-users.png", "/Admin/Users", true],
    ["19-admin-pricing.png", "/Admin/PricingRules", true],
    ["20-admin-topups.png", "/Admin/TopUpRequests", true],
    ["21-admin-reconciliation.png", "/Admin/Reconciliation", true],
    ["22-admin-audit.png", "/Admin/AuditLogs", true],
    ["23-admin-monitoring.png", "/Admin/SystemMonitoring", true],
    ["24-swagger.png", "/swagger", true],
  ];
  for (const [file, url, full] of adminShots) {
    await page.goto(url);
    await page.screenshot(file, full);
  }

  await page.goto("/healthz/db");
  await page.screenshot("25-health-db.png", false);
}

try {
  await main();
} finally {
  chrome.kill();
}
