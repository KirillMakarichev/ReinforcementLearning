using InputManager;

namespace GeometryDashBot;

public class InputManager
{
    public void MousePress(Mouse.MouseKeys mouseButton)
    {
        if(!GDApiManager.GameOnTop()) return;
        
        Mouse.PressButton(mouseButton);
    }
    
    public void MouseDown(Mouse.MouseKeys mouseButton)
    {
        if(!GDApiManager.GameOnTop()) return;

        Mouse.ButtonDown(mouseButton);
    }
    
    public void MouseUp(Mouse.MouseKeys mouseButton)
    {
        if(!GDApiManager.GameOnTop()) return;

        Mouse.ButtonUp(mouseButton);
    }
    
    public void KeyboardPress(Keys key)
    {
        if(!GDApiManager.GameOnTop()) return;

        Keyboard.KeyPress(key);
    }
    
    public void KeyboardDown(Keys key)
    {
        if(!GDApiManager.GameOnTop()) return;

        Keyboard.KeyDown(key);
    }
    
    public void KeyboardUp(Keys key)
    {
        if(!GDApiManager.GameOnTop()) return;

        Keyboard.KeyUp(key);
    }
}