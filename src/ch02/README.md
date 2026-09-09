# ch02 - Setting Up the Project

Chapter 2 of *Building Two Classic Platform Games*.

Open `ch02.slnx` for this chapter alone, or `../PlatformBook.slnx` for all
forty-six, foldered by the book's four parts.

    Both      dotnet build ch02.slnx -c Release
    Android   dotnet build Android/ch02.Android.csproj -c Release -t:Run
    iOS       dotnet build iOS/ch02.iOS.csproj -c Release \
                  -p:RuntimeIdentifier=ios-arm64

Orientation: Portrait (locked)
Shared code in `Game/`, art and audio linked from `../../assets/climber`.
