import requests
import json

# Make a GET request to the website
response = requests.get('https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/dev/data/maps.json')

# Load the JSON data from the website
data = response.json()

# Initialize an empty list for the PreviewImageUrls

# Iterate over each dictionary in the data
for item in data:
    name = item["Name"].lower()
    # Add the PreviewImageUrl to the PreviewImageUrls list
    if "PreviewImageUrl" in item.keys(): del item["PreviewImageUrl"]
    item["ImageUrls"] = {
        "DiscordIcon": f"https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/master/data/images/maps/{name}/DiscordIcon.png",
        "EndGame": f"https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/master/data/images/maps/{name}/EndGame.png",
        "LoadingScreen": f"https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/master/data/images/maps/{name}/LoadingScreen.png",
        "MainMap": f"https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/master/data/images/maps/{name}/MainMap.png",
        "ServerList": f"https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/master/data/images/maps/{name}/ServerList.png",
    }

# Save the PreviewImageUrls list to a JSON file
with open('maps.new.json', 'w') as f:
    json.dump(data, f)
