// Capturas reales de Swagger. Requiere Playwright y Microsoft Edge instalados.
// PLAYWRIGHT_MODULE permite indicar una instalación existente sin modificar el proyecto.
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const out = path.join(root, 'artifacts/evidencias');
const report = JSON.parse(fs.readFileSync(path.join(out, 'resultados.json'), 'utf8'));

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1400, height: 1000 }, deviceScaleFactor: 1 });
    await page.goto('http://localhost:5189/swagger');
    await page.locator('.opblock').first().waitFor();
    // Se capturan los grupos de operaciones, sin manipular sus textos ni respuestas.
    const sections = page.locator('.opblock-tag-section');
    for (const name of ['Contenidos', 'Biblioteca', 'Generos']) {
      const section = sections.filter({ has: page.locator(`h3[data-tag="${name}"]`) });
      await section.screenshot({ path: path.join(out, `swagger-${name.toLowerCase()}.png`) });
    }
    for (const test of [
      { name: '400', method: 'post', route: '/api/auth/register', status: '400', body: { email: 'lector@example.test', password: process.env.Seed__Password } },
      { name: '404', route: '/api/contenidos/{id}', field: 'id', value: '2147483647', status: '404' },
      { name: 'persistencia', route: '/api/contenidos/{id}', field: 'id', value: String(report.reinicio.contenidoId), status: '200' },
    ]) {
      await page.goto('http://localhost:5189/swagger');
      const block = page.locator(`.opblock-${test.method || 'get'}`).filter({ has: page.locator(`[data-path="${test.route}"]`) });
      await block.locator('.opblock-summary-control').click();
      await block.getByRole('button', { name: 'Try it out', exact: true }).click();
      if(test.body) await block.locator('textarea').fill(JSON.stringify(test.body,null,2));
      else await block.locator(`input[placeholder="${test.field}"]`).fill(test.value);
      await block.getByRole('button', { name: 'Execute', exact: true }).click();
      const live = block.locator('.live-responses-table');
      await live.waitFor();
      const status = await live.locator('.response-col_status').last().innerText();
      if (!status.includes(test.status)) throw new Error(`Respuesta inesperada: ${status}`);
      await live.screenshot({ path: path.join(out, `swagger-${test.name}.png`) });
      console.log(`Captura Swagger: ${(test.method || 'get').toUpperCase()} ${test.route} => ${test.status}`);
    }
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exit(1); });
