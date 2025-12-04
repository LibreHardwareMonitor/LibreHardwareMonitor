using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace LibreHardwareMonitor.UI;

/// <summary>
/// https://docs.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-display-option-buttons-in-a-menustrip-windows-forms
/// </summary>
public class ToolStripRadioButtonMenuItem : ToolStripMenuItem
{
    public ToolStripRadioButtonMenuItem()
        : base()
    {
        Initialize();
    }

    public ToolStripRadioButtonMenuItem(string text)
        : base(text, null, (EventHandler)null)
    {
        Initialize();
    }

    public ToolStripRadioButtonMenuItem(Image image)
        : base(null, image, (EventHandler)null)
    {
        Initialize();
    }

    public ToolStripRadioButtonMenuItem(string text, Image image)
        : base(text, image, (EventHandler)null)
    {
        Initialize();
    }

    public ToolStripRadioButtonMenuItem(string text, Image image,
        EventHandler onClick)
        : base(text, image, onClick)
    {
        Initialize();
    }

    public ToolStripRadioButtonMenuItem(string text, Image image,
        EventHandler onClick, string name)
        : base(text, image, onClick, name)
    {
        Initialize();
    }

    public ToolStripRadioButtonMenuItem(string text, Image image,
        params ToolStripItem[] dropDownItems)
        : base(text, image, dropDownItems)
    {
        Initialize();
    }

    public ToolStripRadioButtonMenuItem(string text, Image image,
        EventHandler onClick, Keys shortcutKeys)
        : base(text, image, onClick)
    {
        Initialize();
        ShortcutKeys = shortcutKeys;
    }

    // Called by all constructors to initialize CheckOnClick.
    private void Initialize()
    {
        CheckOnClick = true;
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);

        // If this item is no longer in the checked state or if its
        // parent has not yet been initialized, do nothing.
        if (!Checked || Parent == null) return;

        // Clear the checked state for all siblings.
        foreach (ToolStripItem item in Parent.Items)
        {
            if (item is ToolStripRadioButtonMenuItem radioItem
             && radioItem != this
             && radioItem.Checked)
            {
                radioItem.Checked = false;

                // Only one item can be selected at a time,
                // so there is no need to continue.
                return;
            }
        }
    }

    protected override void OnClick(EventArgs e)
    {
        // If the item is already in the checked state, do not call
        // the base method, which would toggle the value.
        if (Checked) return;

        base.OnClick(e);
    }

    // Let the item paint itself, and then paint the RadioButton
    // where the check mark is normally displayed.
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Image != null)
        {
            // If the client sets the Image property, the selection behavior
            // remains unchanged, but the RadioButton is not displayed and the
            // selection is indicated only by the selection rectangle.
            base.OnPaint(e);
            return;
        }
        else
        {
            // If the Image property is not set, call the base OnPaint method
            // with the CheckState property temporarily cleared to prevent
            // the check mark from being painted.
            CheckState currentState = CheckState;
            CheckState = CheckState.Unchecked;
            base.OnPaint(e);
            CheckState = currentState;
        }

        // Determine the correct state of the RadioButton.
        RadioButtonState buttonState = RadioButtonState.UncheckedNormal;
        if (Enabled)
        {
            if (mouseDownState)
            {
                if (Checked)
                    buttonState = RadioButtonState.CheckedPressed;
                else
                    buttonState = RadioButtonState.UncheckedPressed;
            }
            else if (mouseHoverState)
            {
                if (Checked) 
                    buttonState = RadioButtonState.CheckedHot;
                else
                    buttonState = RadioButtonState.UncheckedHot;
            }
            else
            {
                if (Checked)
                    buttonState = RadioButtonState.CheckedNormal;
            }
        }
        else
        {
            if (Checked)
                buttonState = RadioButtonState.CheckedDisabled;
            else 
                buttonState = RadioButtonState.UncheckedDisabled;
        }

        // Calculate the position at which to display the RadioButton.
        int offset = (ContentRectangle.Height -
                      RadioButtonRenderer.GetGlyphSize(
                                                       e.Graphics, buttonState).Height) / 2;

        Point imageLocation = new Point(
                                        ContentRectangle.Location.X + 4,
                                        ContentRectangle.Location.Y + offset);

        // Paint the RadioButton.
        RadioButtonRenderer.DrawRadioButton(e.Graphics, imageLocation, buttonState);
    }

    private bool mouseHoverState;

    protected override void OnMouseEnter(EventArgs e)
    {
        mouseHoverState = true;

        // Force the item to repaint with the new RadioButton state.
        Invalidate();

        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        mouseHoverState = false;
        base.OnMouseLeave(e);
    }

    private bool mouseDownState;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        mouseDownState = true;

        // Force the item to repaint with the new RadioButton state.
        Invalidate();

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        mouseDownState = false;
        base.OnMouseUp(e);
    }

    // Enable the item only if its parent item is in the checked state
    // and its Enabled property has not been explicitly set to false.
    public override bool Enabled
    {
        get
        {
            // Use the base value in design mode to prevent the designer
            // from setting the base value to the calculated value.
            if (!DesignMode
             && OwnerItem is ToolStripMenuItem ownerMenuItem
             && ownerMenuItem.CheckOnClick)
            {
                return base.Enabled && ownerMenuItem.Checked;
            }
            else
            {
                return base.Enabled;
            }
        }
        set
        {
            base.Enabled = value;
        }
    }

    // When OwnerItem becomes available, if it is a ToolStripMenuItem
    // with a CheckOnClick property value of true, subscribe to its
    // CheckedChanged event.
    protected override void OnOwnerChanged(EventArgs e)
    {
        if (OwnerItem is ToolStripMenuItem ownerMenuItem
         && ownerMenuItem.CheckOnClick)
        {
            ownerMenuItem.CheckedChanged +=
                new EventHandler(OwnerMenuItem_CheckedChanged);
        }
        base.OnOwnerChanged(e);
    }

    // When the checked state of the parent item changes,
    // repaint the item so that the new Enabled state is displayed.
    private void OwnerMenuItem_CheckedChanged(object sender, EventArgs e)
    {
        Invalidate();
    }
}