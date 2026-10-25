# Third-party software

Tunnelka ships with the following third-party components. Each keeps its own license.

## Xray-core

- Version: 26.10.21
- License: Mozilla Public License 2.0 (`core/LICENSE-xray.txt`)
- Source code: https://github.com/XTLS/Xray-core/tree/v26.10.21
- Files: `core/tunnelka-proxy.exe` (unmodified Xray-core executable, renamed), `core/geoip.dat`, `core/geosite.dat`

## sing-box

- Version: 1.14.3
- License: GNU General Public License v3.0 or later (`core/LICENSE-sing-box.txt`)
- Source code: https://github.com/SagerNet/sing-box/tree/v1.14.3
- Files: `core/tunnelka-tun.exe` (unmodified sing-box executable, renamed), `core/libcronet.dll`
- sing-box runs as a separate program. Its unmodified source code for this exact version is available at the link above.

## ZXing.Net

- Version: 0.16.9
- License: Apache License 2.0 (`Assets/ZXing-LICENSE.txt`)
- Source code: https://github.com/micjahn/ZXing.Net

## Twemoji

- Version: 17.0.3
- Graphics (c) Twitter, Inc and other contributors
- License: CC BY 4.0 (`Assets/Emoji-LICENSE.txt`)
- Source: https://github.com/jdecked/twemoji
- Changes: images resized to 64x64 px and palette-reduced, packed into `Assets/emoji.zip`

## Noto Emoji

- License: Apache License 2.0
- Source: https://github.com/googlefonts/noto-emoji
- Used for 19 emoji that Twemoji does not contain yet, resized to 64x64 px and packed into `Assets/emoji.zip`

## circle-flags

- License: MIT (`Assets/Flags-LICENSE.txt`)
- Source: https://github.com/HatScripts/circle-flags

## Avalonia

- Version: 11.2.3 (Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Skia, Avalonia.Win32, Avalonia.Remote.Protocol)
- License: MIT
- Source code: https://github.com/AvaloniaUI/Avalonia

```
Copyright 2013-2024 (c) The AvaloniaUI Project

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## MicroCom.Runtime

- Version: 0.11.0 (used by the UI framework)
- License: MIT
- Source code: https://github.com/kekekeks/MicroCom

```
Copyright 2021 (c) Nikita Tsukanov

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## SkiaSharp and HarfBuzzSharp

- Versions: SkiaSharp 2.88.9, HarfBuzzSharp 7.3.0.3 (with their Windows native assets)
- License: MIT. Their own third-party notices (Skia, HarfBuzz, FreeType, libpng, zlib, ICU and others) are in `Assets/Skia-LICENSE.txt`
- Source code: https://github.com/mono/SkiaSharp

```
Copyright (c) 2015-2016 Xamarin, Inc.
Copyright (c) 2017-2018 Microsoft Corporation.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## ANGLE

- Version: 2.1.22045 (native Windows build used by the UI framework)
- License: BSD 3-Clause

```
// Copyright 2018 The ANGLE Project Authors.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
//     Redistributions of source code must retain the above copyright
//     notice, this list of conditions and the following disclaimer.
//
//     Redistributions in binary form must reproduce the above
//     copyright notice, this list of conditions and the following
//     disclaimer in the documentation and/or other materials provided
//     with the distribution.
//
//     Neither the name of TransGaming Inc., Google Inc., 3DLabs Inc.
//     Ltd., nor the names of their contributors may be used to endorse
//     or promote products derived from this software without specific
//     prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
// COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
```

## System.Drawing.Common and Microsoft.Win32.SystemEvents

- Version: 8.0.8 and 8.0.0
- License: MIT
- Source code: https://github.com/dotnet/winforms and https://github.com/dotnet/runtime

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
