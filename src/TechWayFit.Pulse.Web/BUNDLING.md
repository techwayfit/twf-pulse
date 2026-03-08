# CSS & JavaScript Bundling

## Overview
TechWayFit Pulse uses **BuildBundlerMinifier** to bundle CSS and JavaScript files at build time, reducing HTTP requests and improving page load performance.

## Bundle Configuration

The bundling is configured in `bundleconfig.json` with the following bundles:

### CSS Bundle
- **Output**: `wwwroot/css/pulse-bundle.min.css` (~91 KB)
- **Includes**:
  - `pulse.css` - Design system foundation
  - `pulse-icons.css` - Icon styles
  - `pulse-components.css` - UI components
  - `custom.css` - Custom overrides
  - `mobile.css` - Mobile responsiveness
  - `fullscreen-mode.css` - Fullscreen mode
  - `live-session.css` - Live session styles

### JavaScript Bundles

#### 1. Core Bundle (`pulse-core.min.js` - ~72 KB)
Essential scripts loaded on all pages:
- `qrcodegen.js` - QR code generation
- `qrcode.js` - QR code utilities
- `fullscreen-mode.js` - Fullscreen functionality
- `participant-session.js` - Session management
- `qr-scanner.js` - QR scanning
- `clipboard-helper.js` - Clipboard operations

#### 2. Dashboard Bundle (`pulse-dashboard.min.js` - ~21 KB)
Dashboard and charting scripts:
- `poll-dashboard.js` - Poll chart rendering
- `wordcloud-dashboard.js` - Word cloud visualization
- `quadrant-dashboard.js` - Quadrant matrix display

#### 3. Activities Bundle (`pulse-activities.min.js` - ~42 KB)
Activity management scripts:
- `csv-helper.js` - CSV/TSV parsing
- `activity-modals.js` - Modal dialogs
- `live-activity-manager.js` - Activity state management
- `break-timer.js` - Break timer functionality
- `icon-helper.js` - Icon utilities

## How It Works

1. **Build-Time Bundling**: When you build the project, BuildBundlerMinifier automatically:
   - Combines input files in the specified order
   - Writes output to the specified location
   - Updates bundles only when source files change

2. **Version Caching**: The `asp-append-version="true"` attribute in `_Layout.cshtml` adds a cache-busting query string based on file content hash.

3. **Production Ready**: Bundles are created during both Debug and Release builds.

## Usage in _Layout.cshtml

```html
<!-- CSS Bundle -->
<link rel="stylesheet" href="~/css/pulse-bundle.min.css" asp-append-version="true" />

<!-- JavaScript Bundles -->
<script src="~/js/pulse-core.min.js" asp-append-version="true"></script>
<script src="~/js/pulse-dashboard.min.js" asp-append-version="true"></script>
<script src="~/js/pulse-activities.min.js" asp-append-version="true"></script>
```

## Performance Benefits

### Before Bundling
- **7 CSS files** = 7 HTTP requests
- **14 JS files** = 14 HTTP requests
- **Total**: 21 HTTP requests for static assets

### After Bundling
- **1 CSS bundle** = 1 HTTP request
- **3 JS bundles** = 3 HTTP requests
- **Total**: 4 HTTP requests for static assets

**Result**: ~80% reduction in HTTP requests!

## Modifying Bundles

### Adding a New File
1. Add your CSS/JS file to `wwwroot/css/` or `wwwroot/js/`
2. Edit `bundleconfig.json` and add the file to the appropriate `inputFiles` array
3. Rebuild the project to regenerate bundles

### Creating a New Bundle
1. Add a new entry to `bundleconfig.json`:
```json
{
  "outputFileName": "wwwroot/js/my-bundle.min.js",
  "inputFiles": [
    "wwwroot/js/file1.js",
    "wwwroot/js/file2.js"
  ],
  "minify": {
    "enabled": false
  }
}
```
2. Reference it in `_Layout.cshtml` or page-specific views

### Enabling Minification
Currently, minification is disabled due to compatibility issues with the bundler. To enable:
1. Edit `bundleconfig.json`
2. Change `"enabled": false` to `"enabled": true` for the desired bundle
3. Test thoroughly to ensure no JavaScript errors

## Troubleshooting

### Bundle Not Updating
- Clean and rebuild: `dotnet clean && dotnet build`
- Check `bundleconfig.json` for syntax errors
- Ensure source files exist at the specified paths

### JavaScript Errors After Bundling
- Check browser console for specific errors
- Verify file order in `inputFiles` (dependencies must come first)
- Test with minification disabled first

### Build Errors
- Check Output window for Bundler & Minifier errors
- Common issues:
  - Invalid JSON in `bundleconfig.json`
  - Missing source files
  - CSS @keyframes issues with minification
  - JavaScript syntax errors

## File Order Matters!

JavaScript files are bundled in the order specified. Ensure dependencies come before files that use them:
```json
"inputFiles": [
  "wwwroot/js/library.js",     // Dependency
  "wwwroot/js/app.js"          // Uses library
]
```

## Excluding Files from Bundling

Some files should remain separate:
- **Third-party libraries** loaded from CDN (Bootstrap, Chart.js)
- **Page-specific scripts** that aren't used globally
- **Blazor scripts** (`blazor.server.js`)
- **Development tools** (hot reload, debugging)

## Source Files

Original source files remain in their locations and can be used for:
- Development debugging
- Page-specific overrides
- Individual testing

To use individual files instead of bundles, modify `_Layout.cshtml` to reference them directly.

## CI/CD Considerations

- Bundles are automatically created during `dotnet build`
- No additional build step required
- Bundles are included in publish output
- Works with Azure DevOps, GitHub Actions, and other CI/CD tools

## Future Improvements

1. **Enable Minification**: Once compatibility issues are resolved
2. **Source Maps**: Add `"sourceMap": true` for debugging
3. **Compression**: Consider Brotli/Gzip compression at web server level
4. **Tree Shaking**: Remove unused code from bundles
5. **Lazy Loading**: Split bundles further for on-demand loading

## Additional Resources

- [BuildBundlerMinifier GitHub](https://github.com/madskristensen/BundlerMinifier)
- [ASP.NET Core Bundling Best Practices](https://learn.microsoft.com/en-us/aspnet/core/client-side/bundling-and-minification)
