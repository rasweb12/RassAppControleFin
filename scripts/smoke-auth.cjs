const assert = require("node:assert/strict");
const fs = require("node:fs/promises");
const path = require("node:path");
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || "playwright");
let step = "startup";
(async () => {
    const secrets = JSON.parse(await fs.readFile(path.join(process.env.APPDATA,
        "Microsoft/UserSecrets/RassAppControleFin-Auth-Development/secrets.json"), "utf8").then(text => text.trimStart()));
    const output = path.resolve(__dirname, "../artifacts/smoke");
    await fs.mkdir(output, { recursive: true });
    const browser = await chromium.launch({ headless: true, channel: "msedge" });
    try {
        for (const [name, base] of [["admin", "http://localhost:5082"]]) {
            step = name + " csrf";
            const context = await browser.newContext();
            const rejected = await context.request.post(base + "/auth/session", { form: { email: "x", password: "x" } });
            assert.equal(rejected.status(), 400);
            const page = await context.newPage();
            const errors = [];
            page.on("pageerror", () => errors.push("browser error"));
            const requests = [];
            page.on("request", request => requests.push(request.url()));
            step = name + " signin";
            await page.goto(base + "/login");
            await page.locator('form[action="/auth/session"]').first().locator('[name="email"]').fill(secrets.RASS_AUTH_TEST_EMAIL);
            await page.locator('form[action="/auth/session"]').first().locator('[name="password"]').fill(secrets.RASS_AUTH_TEST_PASSWORD);
            await page.getByRole("button", { name: "Entrar", exact: true }).click();
            step = name + " signin redirect";
            await page.waitForURL(base + "/");
            step = name + " authenticated heading";
            await page.getByRole("heading", { name: name === "web" ? "Contas" : "Status do sistema" }).waitFor();
            step = name + " tenant loading";
            if (name === "web") await page.locator("select option").filter({ hasText: "Pessoal" }).waitFor({ state: "attached" });
            else await page.getByText("Disponivel", { exact: true }).waitFor();
            step = name + " cookie";
            const cookie = (await context.cookies()).find(cookie => cookie.name.startsWith("RassAppControleFin."));
            assert(cookie && cookie.httpOnly && cookie.sameSite === "Strict");
            assert.equal(await page.evaluate(() => localStorage.length + sessionStorage.length), 0);
            assert(!requests.some(url => url.includes(".neonauth.") || url.includes(":5080/api/")));
            assert(!await page.locator("body").innerHTML().then(html => html.includes("AccessToken") || html.includes("SessionToken")));
            for (const viewport of [{ width: 1440, height: 900 }, { width: 390, height: 844 }]) {
                step = name + " layout " + viewport.width;
                await page.setViewportSize(viewport);
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
                await page.screenshot({ path: path.join(output, "auth-" + name + "-" + viewport.width + ".png"), fullPage: true });
            }
            await page.reload();
            await page.getByRole("heading", { name: name === "web" ? "Contas" : "Status do sistema" }).waitFor();
            {
                step = name + " logout";
                await page.getByRole("button", { name: "Sair", exact: true }).click();
                await page.waitForURL(base + "/login");
            }
            assert.deepEqual(errors, []);
            await context.close();
            console.log(name + ": CSRF, login, opaque cookie, persistence, responsive layout OK");
        }
    } finally { await browser.close(); }
})().catch(error => { console.error("Auth smoke failed at " + step + ": " + error.name); process.exitCode = 1; });
