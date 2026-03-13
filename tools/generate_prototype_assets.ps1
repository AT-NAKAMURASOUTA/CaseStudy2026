$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function Get-FontFamily {
    param([string]$PreferredName)

    try {
        return New-Object System.Drawing.FontFamily($PreferredName)
    } catch {
        return [System.Drawing.FontFamily]::GenericSansSerif
    }
}

function New-TransparentBitmap {
    param([int]$Width, [int]$Height)

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $bitmap.MakeTransparent()
    return $bitmap
}

function Save-Png {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [string]$Path
    )

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function New-GuidString {
    return ([guid]::NewGuid().ToString("N"))
}

function Write-UnityFolderMeta {
    param([string]$Path)

    $metaPath = "$Path.meta"
    if (Test-Path $metaPath) {
        return
    }

    @"
fileFormatVersion: 2
guid: $(New-GuidString)
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Set-Content -Path $metaPath -Encoding ASCII
}

function Write-UnityTextMeta {
    param([string]$Path)

    $metaPath = "$Path.meta"
    if (Test-Path $metaPath) {
        return
    }

    @"
fileFormatVersion: 2
guid: $(New-GuidString)
TextScriptImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Set-Content -Path $metaPath -Encoding ASCII
}

function Write-UnityPngMeta {
    param([string]$Path)

    $metaPath = "$Path.meta"
    if (Test-Path $metaPath) {
        return
    }

    @"
fileFormatVersion: 2
guid: $(New-GuidString)
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  spritePackingTag: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Set-Content -Path $metaPath -Encoding ASCII
}

function New-LetterBitmap {
    param(
        [char]$Letter,
        [System.Drawing.FontFamily]$FontFamily,
        [int]$Size = 256
    )

    $bitmap = New-TransparentBitmap -Width $Size -Height $Size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $layoutRect = New-Object System.Drawing.RectangleF(0, 0, 200, 200)
    $stringFormat = [System.Drawing.StringFormat]::GenericDefault
    $path.AddString($Letter, $FontFamily, [int][System.Drawing.FontStyle]::Bold, 180, $layoutRect, $stringFormat)

    $bounds = $path.GetBounds()
    $targetSize = 190.0
    $scale = [Math]::Min($targetSize / $bounds.Width, $targetSize / $bounds.Height)

    $matrix = New-Object System.Drawing.Drawing2D.Matrix
    $matrix.Translate(-$bounds.X, -$bounds.Y)
    $matrix.Scale([float]$scale, [float]$scale)
    $path.Transform($matrix)

    $scaledBounds = $path.GetBounds()
    $centerMatrix = New-Object System.Drawing.Drawing2D.Matrix
    $centerMatrix.Translate(
        [float](($Size - $scaledBounds.Width) / 2 - $scaledBounds.X),
        [float](($Size - $scaledBounds.Height) / 2 - $scaledBounds.Y)
    )
    $path.Transform($centerMatrix)

    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70, 0, 0, 0))
    $fillBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 248, 231, 28))
    $outlinePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 33, 37, 41), 10)
    $outlinePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $shadowMatrix = New-Object System.Drawing.Drawing2D.Matrix
    $shadowMatrix.Translate(8, 8)
    $shadowPath = $path.Clone()
    $shadowPath.Transform($shadowMatrix)

    $graphics.FillPath($shadowBrush, $shadowPath)
    $graphics.FillPath($fillBrush, $path)
    $graphics.DrawPath($outlinePen, $path)

    $shadowPath.Dispose()
    $shadowBrush.Dispose()
    $fillBrush.Dispose()
    $outlinePen.Dispose()
    $path.Dispose()
    $graphics.Dispose()

    return $bitmap
}

function Draw-Limb {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Pen]$Pen,
        [float]$X1,
        [float]$Y1,
        [float]$X2,
        [float]$Y2
    )

    $Graphics.DrawLine($Pen, $X1, $Y1, $X2, $Y2)
}

function New-CharacterSheet {
    param([string]$OutputPath)

    $frameWidth = 96
    $frameHeight = 96
    $frameCount = 4
    $bitmap = New-TransparentBitmap -Width ($frameWidth * $frameCount) -Height $frameHeight
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $bodyBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 76, 175, 80))
    $accentBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 38, 50, 56))
    $highlightBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 245, 157))
    $limbPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 38, 50, 56), 7)
    $limbPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $limbPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    $armSets = @(
        @{ LeftX = 24; LeftY = 46; RightX = 72; RightY = 46; LeftHandX = 12; LeftHandY = 54; RightHandX = 84; RightHandY = 54; LeftLegX = 30; RightLegX = 66 },
        @{ LeftX = 24; LeftY = 46; RightX = 72; RightY = 46; LeftHandX = 8; LeftHandY = 40; RightHandX = 78; RightHandY = 62; LeftLegX = 28; RightLegX = 68 },
        @{ LeftX = 24; LeftY = 46; RightX = 72; RightY = 46; LeftHandX = 18; LeftHandY = 62; RightHandX = 88; RightHandY = 38; LeftLegX = 34; RightLegX = 62 },
        @{ LeftX = 24; LeftY = 46; RightX = 72; RightY = 46; LeftHandX = 14; LeftHandY = 50; RightHandX = 84; RightHandY = 50; LeftLegX = 30; RightLegX = 66 }
    )

    for ($i = 0; $i -lt $frameCount; $i++) {
        $offsetX = $i * $frameWidth
        $pose = $armSets[$i]

        $graphics.FillEllipse($bodyBrush, $offsetX + 30, 10, 36, 36)
        $graphics.FillEllipse($highlightBrush, $offsetX + 41, 22, 6, 6)
        $graphics.FillEllipse($highlightBrush, $offsetX + 49, 22, 6, 6)
        $graphics.FillRectangle($bodyBrush, $offsetX + 28, 42, 40, 26)
        $graphics.FillRectangle($accentBrush, $offsetX + 32, 48, 32, 6)

        Draw-Limb -Graphics $graphics -Pen $limbPen -X1 ($offsetX + $pose.LeftX) -Y1 $pose.LeftY -X2 ($offsetX + $pose.LeftHandX) -Y2 $pose.LeftHandY
        Draw-Limb -Graphics $graphics -Pen $limbPen -X1 ($offsetX + $pose.RightX) -Y1 $pose.RightY -X2 ($offsetX + $pose.RightHandX) -Y2 $pose.RightHandY
        Draw-Limb -Graphics $graphics -Pen $limbPen -X1 ($offsetX + 40) -Y1 68 -X2 ($offsetX + $pose.LeftLegX) -Y2 88
        Draw-Limb -Graphics $graphics -Pen $limbPen -X1 ($offsetX + 56) -Y1 68 -X2 ($offsetX + $pose.RightLegX) -Y2 88
    }

    Save-Png -Bitmap $bitmap -Path $OutputPath

    $bodyBrush.Dispose()
    $accentBrush.Dispose()
    $highlightBrush.Dispose()
    $limbPen.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

$root = Split-Path -Parent $PSScriptRoot
$assetsRoot = Join-Path $root "KeyGame\\Assets\\PrototypeAssets"
$alphabetDir = Join-Path $assetsRoot "Alphabet"
$characterDir = Join-Path $assetsRoot "Characters"

New-Item -ItemType Directory -Force -Path $alphabetDir | Out-Null
New-Item -ItemType Directory -Force -Path $characterDir | Out-Null
Write-UnityFolderMeta -Path $assetsRoot
Write-UnityFolderMeta -Path $alphabetDir
Write-UnityFolderMeta -Path $characterDir

$fontFamily = Get-FontFamily -PreferredName "Arial Black"
$letters = [char[]]"ABCDEFGHIJKLMNOPQRSTUVWXYZ"
$sheetColumns = 6
$sheetRows = [int][Math]::Ceiling($letters.Length / $sheetColumns)
$sheetTileSize = 256
$sheetBitmap = New-TransparentBitmap -Width ($sheetColumns * $sheetTileSize) -Height ($sheetRows * $sheetTileSize)
$sheetGraphics = [System.Drawing.Graphics]::FromImage($sheetBitmap)
$sheetGraphics.Clear([System.Drawing.Color]::Transparent)

for ($i = 0; $i -lt $letters.Length; $i++) {
    $letter = $letters[$i]
    $bitmap = New-LetterBitmap -Letter $letter -FontFamily $fontFamily
    $targetPath = Join-Path $alphabetDir ("{0}.png" -f $letter)
    Save-Png -Bitmap $bitmap -Path $targetPath
    Write-UnityPngMeta -Path $targetPath

    $column = $i % $sheetColumns
    $row = [int][Math]::Floor($i / $sheetColumns)
    $sheetGraphics.DrawImage($bitmap, $column * $sheetTileSize, $row * $sheetTileSize, $sheetTileSize, $sheetTileSize)
    $bitmap.Dispose()
}

Save-Png -Bitmap $sheetBitmap -Path (Join-Path $alphabetDir "AlphabetSheet.png")
Write-UnityPngMeta -Path (Join-Path $alphabetDir "AlphabetSheet.png")
$sheetGraphics.Dispose()
$sheetBitmap.Dispose()

New-CharacterSheet -OutputPath (Join-Path $characterDir "PlaceholderCharacterSheet.png")
Write-UnityPngMeta -Path (Join-Path $characterDir "PlaceholderCharacterSheet.png")

$manifestPath = Join-Path $assetsRoot "README.txt"
@"
Prototype assets generated locally for gameplay testing.

Alphabet
- Individual PNG files A-Z
- AlphabetSheet.png sprite sheet

Character
- PlaceholderCharacterSheet.png with 4 frames

Recommended Unity usage
- Set Texture Type to Sprite (2D and UI)
- For letters, generate a physics shape from alpha or add PolygonCollider2D
- For the character sheet, set Sprite Mode to Multiple and slice by cell size 96x96
"@ | Set-Content -Path $manifestPath -Encoding ASCII
Write-UnityTextMeta -Path $manifestPath
