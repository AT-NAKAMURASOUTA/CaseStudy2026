Prototype assets used by the current prototype scenes.

Alphabet
- Individual PNG files A-Z in Alphabet/SampleLetters/
- AlphabetSheet.png sprite sheet in Alphabet/SampleSheets/

Characters
- PlayerSquare.png for the prototype player sprite in Characters/SamplePlayer/

Common
- PrototypeSquare.png for shared square-based prototype objects such as ground

Current runtime loading
- PrototypePlayerController loads Characters/SamplePlayer/PlayerSquare.png
- PrototypeGroundAuthoring loads Common/PrototypeSquare.png
- PrototypeLetterSpawner loads Alphabet/SampleLetters/A-Z PNG files

Recommended Unity usage
- Set Texture Type to Sprite (2D and UI)
- For letters, use PolygonCollider2D or a generated physics shape from alpha
- Keep prototype assets under PrototypeAssets/ so multiple scenes can reuse them
