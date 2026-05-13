# [JSON Credits](https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.jsoncredits)
by [fiaKaiera](https://github.com/fiaKaiera)

### <img src="https://vcc.docs.vrchat.com/images/favicon.ico" width=24> [Add to VCC / ALCOM](https://fiakaiera.github.io/vpm-listing)
[ [*.unitypackage](https://github.com/fiaKaiera/vpm-listing/releases/download/jsoncredits-1.0.0/net.fiakaiera.jsoncredits-1.0.0.unitypackage) ]
[ [*.zip](https://github.com/fiaKaiera/vpm-listing/releases/download/jsoncredits-1.0.0/net.fiakaiera.jsoncredits-1.0.0.zip) ]

[ [Changelog](https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.jsoncredits/CHANGELOG.md) ] [ [How to Install?](https://github.com/fiaKaiera/vpm-listing/wiki#how-to-install) ]

<a href='https://ko-fi.com/fiaKaiera' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://storage.ko-fi.com/cdn/kofi1.png?v=6' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

> Unity > Package Manager > Add from git URL:
> <br>`https://github.com/fiaKaiera/vpm-listing.git?path=/Packages/net.fiakaiera.jsoncredits`

## Features
- A prefab that allows you to generate a credits list based off a JSON file
- URL link support
- Clicking the help docs leads here :)

## How To Use
> If installing via `*.zip` make sure you extract the contents into a new folder called `net.fiakaiera.jsoncredits` inside your Unity project's `Packages` folder.
1. Install JSON Credits
2. Navigate to "Packages > JSON Credits"
3. Add the "JSON Credits" prefab to the scene
4. Assign a *.json file to the prefab, following the [format](#json-format)
4. Customize to your liking!

## Attribution
Simply put: It would be appreciated if you credit when you use this asset to fiaKaiera.
<br>It can be in any form as long as it is clear and concise.

**Example:**
```
Unique jsoncredits by fiaKaiera
https://github.com/fiaKaiera/vpm-listing
```

It's entirely optional but if you do, it will help spread the word and supports the growth of this asset within the VRChat community.

---

## JSON Format
JSON credits run on a specific format. An example of the format can be found under [Example Credits.json](https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.jsoncredits/Example%20Credits.json).

```json
{
    "Section Name" :
    [
        "Text",
        "---",
        [
            "Main Text",
            "Sub Text",
            "URL"
        ],
        [
            "Main Text",
            "Sub Text",
            "Sub Text 2",
            "URL"
        ],
        [
            "No URL Text",
            ""
        ],
        [
            "Main Text"
        ]
    ]
}
```

The file is a `DataDictionary` with each section containing a `Data List`.
<br>All `String` values that is not the URL is considered Rich Text.

- `Section Name` - dictates a section.
  - `Text` - a `String` that displays text. Multiple instances of these can be added within a section.
  - `---` - a `String` that specifies it as a horizontal separator. Can be changed under [details](#details). Multiple instances of these can be added within a section.
  - `DataList` (`[]`) - an entry that should only contain `String` values.
    - `Main Text` - a `String` that's displayed with a bigger font in an entry. Usually the name of an asset.
    - `Sub Text` - (Optional) a `String` that's displayed with a smaller font in an entry. Usually the asset author. You can have multiple sub text lines, but it's only concatenated as a new line to the same Sub Text object.
    - `URL` - (Optional) the last `String` value in an entry is considered the URL. Cannot be Rich Text. Leave as blank to omit the URL button on the entry.

## Details
The system runs on a "Json Credits" component inside a game object.

- **Credit List:** The credit list in JSON format.
- **Expand First Section:** Expands the first section of the list by default.
- **Separator String:** The string that changes the text into a separator. (Default: "---")

Several fields refrencing parts of the prefab can only be seen through the debug inspector. It's advisable for you not to change them unless you know what you are doing.

### Json Credits Section
`JsonCreditsSection` is a component that attaches to a section's button. It gives a signal to the main Json Credits component to refresh the list. Some of these references are deleted after initialization since they are no longer used after its creation.

---

## Issues? Feature Requests?
Best report them through the [Issues](https://github.com/fiaKaiera/vpm-listing/issues) tab.
<br>If you are savvy enough, then you can try making a [pull request](https://github.com/fiaKaiera/vpm-listing/pulls) fixing the issue.

## Credits
- Unique jsoncredits by [fiaKaiera](https://github.com/fiaKaiera)
- Icon: `material-symbols:featured-play-list-rounded` and `material-symbols:link-rounded` from [Material Symbols](https://github.com/google/material-design-icons), fetched from [Icônes](https://icones.js.org/collection/material-symbols)

## Inspiration
The system is created first for a world called HopCat Hometown (private world) where multiple residents live in the same instance.
<br>However usual credit list assets available
