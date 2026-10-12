const assert = require("node:assert/strict");
const fs = require("node:fs/promises");
const path = require("node:path");
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || "playwright");

(async () => {
    const output = path.resolve(__dirname, "../artifacts/smoke");
    await fs.mkdir(output, { recursive: true });
    let browser;
    try {
        browser = await chromium.launch({ headless: true, channel: "msedge" });
    } catch {
        browser = await chromium.launch({ headless: true });
    }
    try {
        for (const [name, url] of [
            ["admin", process.env.ADMIN_URL || "http://localhost:5082"]
        ]) {
            for (const viewport of [{ width: 1440, height: 900 }, { width: 390, height: 844 }]) {
                const page = await browser.newPage({ viewport });
                const errors = [];
                page.on("pageerror", error => errors.push(error.message));
                await page.goto(url);
                await page.getByText("Disponivel", { exact: true }).waitFor();
                await page.getByRole("button", { name: "Atualizar status" }).click();
                await page.getByText("Disponivel", { exact: true }).waitFor();
                assert.equal(await page.evaluate(() =>
                    document.documentElement.scrollWidth <= window.innerWidth), true);
                const iconLoaded = await page.locator("button img").evaluate(image => image.complete && image.naturalWidth > 0);
                assert.equal(iconLoaded, true);
                assert.deepEqual(errors, []);
                await page.screenshot({ path: path.join(output, name + "-" + viewport.width + ".png"), fullPage: true });
                await page.close();
                console.log(name + " " + viewport.width + ": refresh, icon and layout OK");
            }
        }
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
