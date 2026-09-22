using Godot;
using System;
public partial class DimensionGroup : Node2D
{
    private int currentDimension = 0;

    public override void _Ready()
    {
        UpdateVisibility();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("switch_dimension"))
        {
            currentDimension = currentDimension + 1;

            if (currentDimension >= GetChildCount())
            {
                currentDimension = 0;
            }

            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        for (int i = 0; i < GetChildCount(); i++)
        {
            Node2D child = GetChild<Node2D>(i);

            if (i == currentDimension)
            {
                child.Visible = true;
                child.ProcessMode = ProcessModeEnum.Inherit;
            }
            else
            {
                child.Visible = false;
                child.ProcessMode = ProcessModeEnum.Disabled;
            }
        }
    }
}

