# ch30 - Running Movement

Chapter 30 of *Building Two Classic Platform Games*.

Open `ch30.slnx` for this chapter alone, or `../PlatformBook.slnx` for all
forty-six, foldered by the book's four parts.

    Both      dotnet build ch30.slnx -c Release
    Android   dotnet build Android/ch30.Android.csproj -c Release -t:Run
    iOS       dotnet build iOS/ch30.iOS.csproj -c Release \
                  -p:RuntimeIdentifier=ios-arm64

Orientation: Landscape (locked)
Shared code in `Game/`, art and audio linked from `../../assets/hunch`.
