# ch09 - Building the Audio Engine

Chapter 9 of *Building Two Classic Platform Games*.

Open `ch09.slnx` for this chapter alone, or `../PlatformBook.slnx` for all
forty-six, foldered by the book's four parts.

    Both      dotnet build ch09.slnx -c Release
    Android   dotnet build Android/ch09.Android.csproj -c Release -t:Run
    iOS       dotnet build iOS/ch09.iOS.csproj -c Release \
                  -p:RuntimeIdentifier=ios-arm64

Orientation: Portrait (locked)
Shared code in `Game/`, art and audio linked from `../../assets/climber`.
