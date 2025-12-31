# Assets

This directory contains visual assets for the MicPassthrough project.

## Files

- **logo.png** - Main project logo (used in README header)
- **icon.png** - Application icon (292KB, original resolution)
- **icon-16x16.png** - 16×16 resolution (1.1KB)
- **icon-32x32.png** - 32×32 resolution (2.3KB)
- **icon-64x64.png** - 64×64 resolution (5.7KB)
- **icon-128x128.png** - 128×128 resolution (16KB)
- **icon-256x256.png** - 256×256 resolution (43KB)

## Icon Resolutions

All standard resolutions have been generated from the original `icon.png`:

| File | Size | Use Case |
|------|------|----------|
| icon-16x16.png | 1.1KB | Favicon, taskbar |
| icon-32x32.png | 2.3KB | Application icon, toolbar |
| icon-64x64.png | 5.7KB | Large toolbar, settings |
| icon-128x128.png | 16KB | Windows Explorer, thumbnail |
| icon-256x256.png | 43KB | Desktop icon, store listing |

These were generated using ImageMagick with LANCZOS filtering for high-quality downsampling.

### Creating Icon Resolutions

If you need to regenerate or create additional resolutions, use these tools:

**ImageMagick (command line):**
```bash
magick icon.png -resize 16x16 icon-16x16.png
magick icon.png -resize 32x32 icon-32x32.png
magick icon.png -resize 64x64 icon-64x64.png
magick icon.png -resize 128x128 icon-128x128.png
magick icon.png -resize 256x256 icon-256x256.png
```

### Converting to ICO Format

For use as a Windows executable icon or shortcut, convert to ICO format:

**ImageMagick:**
```bash
magick icon.png icon.ico
```

**Online:**
- [ConvertIO.co](https://convertio.co/png-ico/) - PNG to ICO converter

Once created, place resized versions and ICO formats in this directory.
