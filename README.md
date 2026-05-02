# ValkyrieLib

## API
### ValkyreAPI
`ValkyrieAPI.GetHandle(Mod)` - Exposes mod related API through `ModHandle`.  

### ModHandle
`RegisterUI(string, string, Func<UIState>)` - Register a new ui element.  
`RegisterKeybind(string, string, Action)` - Register a new keybind tied to an action.  
`Log(object)` - Log something to `Logs.txt` in root.  

### Classes
`HBoxContainer` - Horizontally auto sizes elements for you.  
`VBoxContainer` - Vertically auto sizes elements for you.  
`Button`  

### Components
`InputField`  
`Slider`  
`RgbSlider`  
`HsvSlider`  

### Interfaces
`IBlocksInput`  
`IHasMainPanel`  
`IHasCloseButton`  
`IHasScrollbar`  

## Example Usage
```cs
public class YourMod : Mod
{
    public override void Load()
    {
        var modHandle = ValkyrieAPI.GetHandle(this);

        modHandle.RegisterUI("My Panel", "H", () => new MyCustomPanel());
        modHandle.RegisterKeybind("Hello", "K", () => Main.NewText("Hello!"));
    }
}
```

```cs
public class MyCustomPanel : UIState, IBlocksInput, IHasCloseButton
{
    public UIElement MainElement { get; private set; }

    public event Action CloseRequested;

    public override void OnInitialize()
    {
        var panel = new UIPanel();
        panel.Width.Set(300, 0f);
        panel.Height.Set(150, 0f);
        panel.BackgroundColor = Main.OurFavoriteColor;
        panel.HAlign = 0.5f;
        panel.VAlign = 0.5f;
        Append(panel);

        var vbox = new VBoxContainer();
        vbox.Append(/*define element1 here*/);
        vbox.Append(/*define element2 here*/);
        panel.Append(vbox);

        MainElement = panel;
    }
}
```
