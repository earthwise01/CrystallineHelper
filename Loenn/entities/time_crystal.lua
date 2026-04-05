local crystallineHelper = require("mods").requireFromPlugin("libraries.crystalline_helper")

local timeCrystal = {}

timeCrystal.name = "vitellary/timecrystal"
timeCrystal.depth = -100

timeCrystal.fieldInformation = {
    stopLength = {
        minimumValue = 0.0
    },
    respawnTime = {
        minimumValue = 0.0
    },
    timeScale = {
        minimumValue = 0.0
    },
    entityTypesToIgnore = {
        fieldType = "list",
        elementDefault = "",
        elementOptions = {
             options = function() return crystallineHelper.getMapSIDs() end,
             searchable = true
        }
    }
}

timeCrystal.placements = {
    {
        name = "normal",
        data = {
            oneUse = false,
            stopLength = 2.0,
            respawnTime = 2.5,
            untilDash = false,
            immediate = false,
            entityTypesToIgnore = "",
            timeScale = 0.0
        }
    },
    {
        name = "until_dash",
        data = {
            oneUse = false,
            stopLength = 2.0,
            respawnTime = 2.5,
            untilDash = true,
            immediate = false,
            entityTypesToIgnore = "",
            timeScale = 0.0
        }
    }
}

function timeCrystal.texture(room, entity)
    return entity.untilDash and "objects/crystals/time/untildash/idle00" or "objects/crystals/time/idle00"
end

return timeCrystal
