const fs = require('fs-extra');
const concat = require('concat');
const path = require('path');

(async function build() {
    // Angular 19 builds to dist/<project_name>/browser by default.
    // Let's ensure the path is correct.
    const distPath = path.join(__dirname, 'dist/enterprise-ai.ui/browser');
    const outPath = path.join(__dirname, 'dist/enterprise-chatbot');
    
    // Create output directory if it doesn't exist
    await fs.ensureDir(outPath);

    // Files to concatenate. Note: check if they exist first, main.js and polyfills.js are standard.
    const files = [
        path.join(distPath, 'polyfills.js'),
        path.join(distPath, 'main.js')
    ];

    // Filter files to only include those that actually exist (in case of different build configs)
    const existingFiles = files.filter(file => fs.existsSync(file));

    if (existingFiles.length === 0) {
        console.error('Build failed: No polyfills.js or main.js found in ' + distPath);
        process.exit(1);
    }

    // Concatenate JS files with IIFE wrappers to avoid variable collisions
    let combinedJs = '';
    for (const file of existingFiles) {
        const content = await fs.readFile(file, 'utf8');
        // Wrap each file in an IIFE so their top-level minified variables don't conflict
        combinedJs += `\n(() => {\n${content}\n})();\n`;
    }
    
    await fs.writeFile(path.join(outPath, 'enterprise-chatbot.js'), combinedJs);
    console.log('Successfully concatenated JS files into enterprise-chatbot.js');

    // Copy CSS if it exists
    const cssFile = path.join(distPath, 'styles.css');
    if (fs.existsSync(cssFile)) {
        await fs.copy(cssFile, path.join(outPath, 'enterprise-chatbot.css'));
        console.log('Successfully copied styles.css to enterprise-chatbot.css');
    } else {
        console.warn('Warning: styles.css not found in ' + distPath);
    }

    // Copy any assets if needed (Optional)
    const assetsSrcPath = path.join(distPath, 'assets');
    const assetsDestPath = path.join(outPath, 'assets');
    if (fs.existsSync(assetsSrcPath)) {
        await fs.copy(assetsSrcPath, assetsDestPath);
        console.log('Successfully copied assets');
    }
})();
