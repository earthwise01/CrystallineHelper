local loadedState = require("loaded_state")
local entities = require("entities")

local crystallineHelper = {}

function crystallineHelper.getAllSIDs()
    local sids = {}
    for k, v in pairs(entities.registeredEntities) do
        table.insert(sids, k)
    end
    table.sort(sids)

    return sids
end

function crystallineHelper.getMapSIDs()
    if not loadedState.map then return crystallineHelper.getAllSIDs() end

    local sidsInMap = {}
    for _, room in pairs(loadedState.map.rooms) do
        for _, entity in pairs(room.entities) do
            sidsInMap[entity._name] = true
        end
    end

    local sids = {}
    for k, v in pairs(sidsInMap) do
        table.insert(sids, k)
    end
    table.sort(sids)

    return sids
end

return crystallineHelper
