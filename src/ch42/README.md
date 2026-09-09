# ch42 - Polishing the Runner

Chapter 42 of *Building Two Classic Platform Games*.

Open `ch42.slnx` for this chapter alone, or `../PlatformBook.slnx` for all
forty-six, foldered by the book's four parts.

    Both      dotnet build ch42.slnx -c Release
    Android   dotnet build Android/ch42.Android.csproj -c Release -t:Run
    iOS       dotnet build iOS/ch42.iOS.csproj -c Release \
                  -p:RuntimeIdentifier=ios-arm64

Orientation: Portrait (locked)
Shared code in `Game/`, art and audio linked from `../../assets/hunch`.
