const { chromium } = require(process.argv[2]);

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: process.argv[3] });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, acceptDownloads: true });
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  const check = (condition, label) => { if (!condition) throw new Error(`Failed: ${label}`); };

  await page.goto('file:///C:/Users/abd82/Desktop/prototypejsup/prototype.html', { waitUntil: 'load' });
  await page.evaluate(() => localStorage.clear());
  await page.reload({ waitUntil: 'load' });
  await page.waitForTimeout(250);
  await page.evaluate(() => navigate('explore'));

  await page.click('.multi-select-trigger');
  check(await page.locator('.multi-course-option').count() === 4, 'course picker lists all courses for current major');
  await page.locator('.multi-course-option').nth(1).click();
  check((await page.evaluate(() => state.selectedCourseIds)).join(',') === '1,2', 'second course can be selected');
  check(await page.locator('.selected-course-chip').count() === 2, 'selected courses shown as chips');
  check(await page.locator('.resource-card').count() === 16, 'resources aggregate across selected courses');

  await page.selectOption('#filter-major', '2');
  await page.click('.multi-select-trigger');
  await page.locator('.multi-course-option').first().click();
  check((await page.evaluate(() => state.selectedCourseIds)).join(',') === '1,2,4', 'course selection persists across majors');
  check(await page.locator('.resource-card').count() === 24, 'resources aggregate across three courses');
  await page.locator('.selected-course-chip').nth(1).locator('button').first().click();
  check(await page.evaluate(() => state.courseId) === 2, 'selected course can become active');

  const demoDownloadEvent = page.waitForEvent('download');
  await page.locator('.download-link').first().click();
  const demoDownload = await demoDownloadEvent;
  check(demoDownload.suggestedFilename().endsWith('.txt'), 'demo resource downloads a real file');

  await page.locator('.preview-link').first().click();
  check((await page.locator('.download-ready').innerText()).includes('Ready to download'), 'drawer shows file availability');
  const drawerDownloadEvent = page.waitForEvent('download');
  await page.locator('#resource-drawer-foot button').first().click();
  const drawerDownload = await drawerDownloadEvent;
  check(drawerDownload.suggestedFilename().endsWith('.txt'), 'drawer download works');
  await page.evaluate(() => closePanels());

  const fontSizes = await page.evaluate(() => ({
    body: parseFloat(getComputedStyle(document.body).fontSize),
    resourceMeta: parseFloat(getComputedStyle(document.querySelector('.resource-sub')).fontSize),
    fieldLabel: parseFloat(getComputedStyle(document.querySelector('.field label')).fontSize),
    resourceTitle: parseFloat(getComputedStyle(document.querySelector('.resource-card h3')).fontSize)
  }));
  check(fontSizes.body >= 15 && fontSizes.resourceMeta >= 12 && fontSizes.fieldLabel >= 12 && fontSizes.resourceTitle >= 17, 'readability font sizes applied');

  await page.evaluate(() => navigate('contribute'));
  await page.setInputFiles('#upload-file', { name: 'original-notes.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4 original demo file') });
  await page.getByRole('button', { name: /Continue/ }).click();
  await page.fill('#upload-title', 'Original downloadable notes');
  await page.getByRole('button', { name: /Review/ }).click();
  await page.check('#upload-consent');
  await page.getByRole('button', { name: /Publish resource/ }).click();
  await page.waitForTimeout(1500);
  await page.getByRole('button', { name: /View resource/ }).click();
  await page.waitForTimeout(100);
  check((await page.locator('.download-ready').innerText()).includes('Original uploaded file'), 'uploaded original is recognized');
  const originalDownloadEvent = page.waitForEvent('download');
  await page.locator('#resource-drawer-foot button').first().click();
  const originalDownload = await originalDownloadEvent;
  check(originalDownload.suggestedFilename() === 'original-notes.pdf', 'original uploaded file downloads with original name');
  await page.evaluate(() => closePanels());

  const widths = {};
  for (const width of [390, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.evaluate(() => navigate('explore'));
    await page.waitForTimeout(70);
    widths[width] = await page.evaluate(() => ({ page: document.documentElement.scrollWidth, viewport: innerWidth }));
    check(widths[width].page <= widths[width].viewport, `no horizontal overflow at ${width}`);
  }
  await page.setViewportSize({ width: 390, height: 844 });
  const mobileFont = await page.locator('.mobile-link').first().evaluate(node => parseFloat(getComputedStyle(node).fontSize));
  check(mobileFont >= 9, 'mobile navigation text is readable');
  check(errors.length === 0, 'no runtime errors');

  console.log(JSON.stringify({ passed: true, selectedCourseIds: await page.evaluate(() => state.selectedCourseIds), fontSizes, widths, errors }, null, 2));
  await browser.close();
})().catch(error => { console.error(error.stack || error.message); process.exit(1); });
