# ValkyrieLib

## API
### Classes
`HBoxContainer` - Horizontally auto sizes elements for you.  
`VBoxContainer` - Vertically auto sizes elements for you.  

### Components
`InputField`  
`Slider`  
`RgbSlider`  
`HsvSlider`  

### Interfaces
`IClosable` - This element is closable.  
`IInputConsumer` - This element consumes mouse / keyboard inputs.  

### Methods
`ValkyrieAPI.GetHandle(Mod)` - Exposes mod related API through `ModHandle`.  
`ValkyrieAPI.UI.CreateCloseButton()` - Creates a close button docked at the top right.  
`modHandle.RegisterUI(string, string, Func<UIState>)` - Register a new ui element.  
`modHandle.RegisterKeybind(string, string, Action)` - Register a new keybind tied to an action.  

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
public class MyCustomPanel : UIState, IInputConsumer, IClosable
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
        
        var closeButton = /*define close btn here*/;
        closeButton.OnLeftClick += (_, _) => CloseRequested?.Invoke();
        panel.Append(closeButton);

        MainElement = panel;
    }
}
```
