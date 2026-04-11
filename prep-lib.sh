#!/bin/bash

# Check if VINTAGE_STORY is set
if [ -z "$VINTAGE_STORY" ]; then
    echo "Error: VINTAGE_STORY environment variable is not set."
    exit 1
fi

echo "Copying Vintage Story binaries from: $VINTAGE_STORY"

# Create target directories
mkdir -p lib/Mods
mkdir -p lib/Lib

# 1. Root DLLs
cp "$VINTAGE_STORY/VintagestoryAPI.dll" lib/
cp "$VINTAGE_STORY/VintagestoryLib.dll" lib/

# 2. Mods Folder DLLs
cp "$VINTAGE_STORY/Mods/VSSurvivalMod.dll" lib/Mods/
cp "$VINTAGE_STORY/Mods/VSEssentials.dll" lib/Mods/
cp "$VINTAGE_STORY/Mods/VSCreativeMod.dll" lib/Mods/

# 3. Lib Folder DLLs
cp "$VINTAGE_STORY/Lib/Newtonsoft.Json.dll" lib/Lib/
cp "$VINTAGE_STORY/Lib/0Harmony.dll" lib/Lib/
cp "$VINTAGE_STORY/Lib/protobuf-net.dll" lib/Lib/
cp "$VINTAGE_STORY/Lib/cairo-sharp.dll" lib/Lib/
cp "$VINTAGE_STORY/Lib/Microsoft.Data.Sqlite.dll" lib/Lib/

echo "Done! The /lib folder is now ready for building."