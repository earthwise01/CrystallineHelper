local crystallineHelper = require("mods").requireFromPlugin("libraries.crystalline_helper")

-- stolen from consts.object_depths.lua, edited to look better in Lönn.
-- see: https://github.com/CelestialCartographers/Loenn/blob/master/src/consts/object_depths.lua
local depths = {
    {"BG Terrain (10000)", 10000},
    {"BG Mirrors (9500)", 9500},
    {"BG Decals (9000)", 9000},
    {"BG Particles (8000)", 8000},
    {"Solids Below (5000)", 5000},
    {"Below (2000)", 2000},
    {"NPCs (1000)", 1000},
    {"Theo Crystal (100)", 100},
    {"Player (0)", 0},
    {"Dust (-50)", -50},
    {"Pickups (-100)", -100},
    {"Seeker (-200)", -200},
    {"Particles (-8000)", -8000},
    {"Above (-8500)", -8500},
    {"Solids (-9000)", -9000},
    {"FG Terrain (-10000)", -10000},
    {"FG Decals (-10500)", -10500},
    {"Dream Blocks (-11000)", -11000},
    {"Crystal Spinners (-11500)", -11500},
    {"Player Dream Dashing (-12000)", -12000},
    {"Enemy (-12500)", -12500},
    {"Fake Walls (-13000)", -13000},
    {"FG Particles (-50000)", -50000},
    {"Top (-1000000)", -1000000},
    {"Formation Sequences (-2000000)", -2000000}
}

local editDepthTrigger = {}

editDepthTrigger.name = "vitellary/editdepthtrigger"

editDepthTrigger.fieldOrder = {
    "x", "y", "width", "height",
    "depth", "entitiesToAffect",
    "debug", "updateOnEntry", "cacheValidEntities", "initializeInAwake"
}

editDepthTrigger.fieldInformation = {
    depth = {
        options = depths
    },
    entitiesToAffect = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return crystallineHelper.getMapSIDs() end,
             searchable = true
        }
    }
}

editDepthTrigger.placements = {
    {
        name = "edit_depth_trigger",
        data = {
            depth = -9000,
            entitiesToAffect = "moveBlock",
            debug = false,
            initializeInAwake = true,
            updateOnEntry = false,
            cacheValidEntities = true,
        }
    }
}

return editDepthTrigger
