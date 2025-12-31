# Assets

This directory contains visual assets for the MicPassthrough project.

## Files

- **logo.png** - Main project logo (used in README header)
- **icon.png** - Application icon (292KB, original resolution)

## Icon Resolutions

The `icon.png` file is the primary icon asset. To create different resolutions for various uses, the following sizes are recommended:

| Resolution | Use Case | Command |
|-----------|----------|---------|
| 16x16 | Favicon, taskbar | See below |
| 32x32 | Application icon, toolbar | See below |
| 64x64 | Large toolbar, settings | See below |
| 128x128 | Windows Explorer, thumbnail | See below |
| 256x256 | Desktop icon, store listing | See below |

### Creating Icon Resolutions

To generate these resolutions, you can use any of these tools:

**Option 1: ImageMagick (command line)**
```bash
magick icon.png -resize 16x16 icon-16x16.png
magick icon.png -resize 32x32 icon-32x32.png
magick icon.png -resize 64x64 icon-64x64.png
magick icon.png -resize 128x128 icon-128x128.png
magick icon.png -resize 256x256 icon-256x256.png
```

**Option 2: Online tool**
- [Imageresizer.com](https://imageresizer.com/) - Batch resize images
- [FreeConvert.com](https://freeconvert.com/image-converter) - Image conversion

**Option 3: Windows batch processing**
- GIMP - Open source image editor
- Paint.NET - Free Windows image editor
- Photoshop - Professional tool

### Converting to ICO Format

For use as a Windows executable icon or shortcut, convert to ICO format:

**ImageMagick:**
```bash
magick icon.png icon.ico
```

**Online:**
- [ConvertIO.co](https://convertio.co/png-ico/) - PNG to ICO converter

Once created, place resized versions and ICO formats in this directory.
