System.Text.RegularExpressions.Regex surfaceIdMatcher = new System.Text.RegularExpressions.Regex(
    @"\s+<(\d+)>$",
    System.Text.RegularExpressions.RegexOptions.Compiled
);
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<string> sprites = new List<string>();

public enum DrawableType {
    TEXT,
    SPLIT,
    BAR
};

public class Drawable {
    public Dictionary<DrawableType, List<string>> lines;

    public Drawable() {
        lines = new Dictionary<DrawableType, List<string>>();
    }
}

public struct CharInfo {
    public CharInfo(Vector2 fontSize, int charsX, int charsY) {
        x = charsX;
        y = charsY;
        cx = fontSize.X;
        cy = fontSize.Y;
    }

    public override string ToString() => $"({x}, {y}, {cx}, {cy})";
}

public CharInfo CharsPerWidth(IMyTextSurface surface, float padding = 0f) {
    StringBuilder sb = new StringBuilder(" ");
    Vector2 charSizeInPx = surface.MeasureStringInPixels(sb, surface.Font, surface.FontSize);

    return new VectCharInfor4(
        charSizeInPx,
        (int)Math.Floor((surface.SurfaceSize.X * (1 - (padding / 100))) / charSizeInPx.X),
        (int)Math.Floor((surface.SurfaceSize.Y * (1 - (padding / 100))) / charSizeInPx.Y)
    );
}

public void WriteTextToSurface(IMyTextSurface surface, Drawable drawable) {
    surface.ContentType = ContentType.SCRIPT;
    surface.Script = "";
    surface.Font = "Monospace";

    RectangleF viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
    MySpriteDrawFrame frame = surface.DrawFrame();
    Vector2 position = new Vector2(0, 0) + viewport.Position;
    // CharInfo chars = CharsPerWidth(surface);

    MySprite sprite;
    foreach (var toDraw in drawable.lines) {
        if (toDraw.Key == DrawableType.TEXT || toDraw.Key == DrawableType.SPLIT) {
            string text = toDraw.Value[0];
            if (toDraw.Key == DrawableType.SPLIT) {
                //
            }

            sprite = new MySprite() {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = surface.FontSize,
                Color = surface.FontColor,
                Alignment = TextAlignment.LEFT,
                FontId = surface.Font
            };

            //
        } else if (toDraw.Key == DrawableType.BAR) {
        } else if (toDraw.Key == DrawableType.SPLIT) {
            //
        }
        frame.Add(sprite);
    }
    frame.Dispose();
}

// public IMyTerminalBlock GetBlockWithName(string name) {
//     blocks.Clear();
//     GridTerminalSystem.SearchBlocksOfName(name, blocks, c => c.CubeGrid == Me.CubeGrid && c.CustomName == name);
//     if (blocks.Count != 1) {
//         return null;
//     }

//     return blocks[0];
// }

// public IMyTextSurface GetBlockSurface(string blockName) {
//     if (blockName == "") {
//         return null;
//     }

//     IMyTextPanel panel;
//     int surfaceId = 0;

//     var matches = surfaceIdMatcher.Matches(blockName);
//     if (matches.Count > 0 && matches[0].Groups.Count > 1) {
//         if (!Int32.TryParse(matches[0].Groups[1].Value, out surfaceId)) {
//            surfaceId = 0;
//         }
//         string panelName = blockName.Replace(matches[0].Groups[0].Value, "");
//         panel = (IMyTextPanel)GetBlockWithName(panelName);
//     } else {
//         panel = (IMyTextPanel)GetBlockWithName(blockName);
//     }

//     if (panel == null || !(panel is IMyTextPanel || panel is IMyTextSurfaceProvider)) {
//         return null;
//     }

//     IMyTextSurface surface = panel is IMyTextSurface
//         ? (IMyTextSurface)panel
//         : ((IMyTextSurfaceProvider)panel).GetSurface(surfaceId);

//     return surface;
// }

public void Main() {
    Drawable drawble = new Drawable();
    drawble.lines.Add()

    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            WriteTextToSurface(block.GetSurface(i));
        }
    }
}
