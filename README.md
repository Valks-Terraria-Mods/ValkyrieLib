# ValkyrieLib

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
public class MyCustomPanel : UIState, IHasMainElement, IClosable
{
    // Implement IHasMainElement to consume input when the mouse hovers over this element
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

        // VBoxContainer (and HBoxContainer) handle sizing for you automatically
        var vbox = new VBoxContainer();
        vbox.Append(element1);
        vbox.Append(element2);
        panel.Append(vbox);

        var closeButton = UiControlFactory.CloseBtn();
        closeButton.OnLeftClick += (_, _) => CloseRequested?.Invoke();
        panel.Append(closeButton);

        MainElement = panel;
    }
}
```
