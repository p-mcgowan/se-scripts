// Decompiled with JetBrains decompiler
// Type: Sandbox.Game.GameSystems.TextSurfaceScripts.MyTSSEnergyHydrogen
// Assembly: Sandbox.Game, Version=0.1.7058.25453, Culture=neutral, PublicKeyToken=null
// MVID: 78A37FCC-88C1-4EF0-8C3B-0CF1E189CCDA
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64\Sandbox.Game.dll

using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Sandbox.Game.GameSystems.TextSurfaceScripts
{
  [MyTextSurfaceScript("TSS_EnergyHydrogen", "DisplayName_TSS_EnergyHydrogen")]
  public class MyTSSEnergyHydrogen : MyTSSCommon
  {
    public static float ASPECT_RATIO = 3f;
    public static float DECORATION_RATIO = 0.25f;
    public static float TEXT_RATIO = 0.25f;
    public static string ENERGY_ICON = "IconEnergy";
    public static string HYDROGEN_ICON = "IconHydrogen";
    private StringBuilder m_sb = new StringBuilder();
    private List<Sandbox.Game.Entities.Interfaces.IMyGasTank> m_tankBlocks = new List<Sandbox.Game.Entities.Interfaces.IMyGasTank>();
    private Vector2 m_innerSize;
    private Vector2 m_decorationSize;
    private float m_firstLine;
    private float m_secondLine;
    private MyResourceDistributorComponent m_resourceDistributor;
    private MyCubeGrid m_grid;
    private float m_maxHydrogen;

    public override ScriptUpdate NeedsUpdate
    {
      get
      {
        return ScriptUpdate.Update10;
      }
    }

    public MyTSSEnergyHydrogen(Sandbox.ModAPI.IMyTextSurface surface, VRage.Game.ModAPI.IMyCubeBlock block, Vector2 size)
      : base((Sandbox.ModAPI.Ingame.IMyTextSurface) surface, (VRage.Game.ModAPI.Ingame.IMyCubeBlock) block, size)
    {
      this.m_innerSize = new Vector2(MyTSSEnergyHydrogen.ASPECT_RATIO, 1f);
      MyTextSurfaceScriptBase.FitRect(size, ref this.m_innerSize);
      this.m_decorationSize = new Vector2(0.012f * this.m_innerSize.X, MyTSSEnergyHydrogen.DECORATION_RATIO * this.m_innerSize.Y);
      this.m_sb.Clear();
      this.m_sb.Append("Power Usage: 00.000");
      Vector2 vector2 = MyGuiManager.MeasureStringRaw(this.m_fontId, this.m_sb, 1f);
      float val2 = MyTSSEnergyHydrogen.TEXT_RATIO * this.m_innerSize.Y / vector2.Y;
      this.m_fontScale = Math.Min(this.m_innerSize.X * 0.72f / vector2.X, val2);
      this.m_firstLine = this.m_halfSize.Y - this.m_decorationSize.Y * 0.55f;
      this.m_secondLine = this.m_halfSize.Y + this.m_decorationSize.Y * 0.55f;
      if (this.m_block == null)
        return;
      this.m_grid = this.m_block.CubeGrid as MyCubeGrid;
      if (this.m_grid == null)
        return;
      this.m_resourceDistributor = this.m_grid.GridSystems.ResourceDistributor;
      this.m_grid.GridSystems.ConveyorSystem.BlockAdded += new Action<MyCubeBlock>(this.ConveyorSystemOnBlockAdded);
      this.m_grid.GridSystems.ConveyorSystem.BlockRemoved += new Action<MyCubeBlock>(this.ConveyorSystemOnBlockRemoved);
      this.Recalculate();
    }

    public override void Run()
    {
      base.Run();
      using (MySpriteDrawFrame frame = this.m_surface.DrawFrame())
      {
        this.AddBackground(frame, new Color?(new Color(this.m_foregroundColor, 0.66f)));
        if (this.m_resourceDistributor == null && this.m_grid != null)
          this.m_resourceDistributor = this.m_grid.GridSystems.ResourceDistributor;
        if (this.m_resourceDistributor == null)
          return;
        Color barBgColor = new Color(this.m_foregroundColor, 0.1f);
        float x = this.m_innerSize.X * 0.5f;
        float num1 = x * 0.06f;
        float max = this.m_resourceDistributor.MaxAvailableResourceByType(MyResourceDistributorComponent.ElectricityId);
        float num2 = MyMath.Clamp(this.m_resourceDistributor.TotalRequiredInputByType(MyResourceDistributorComponent.ElectricityId), 0.0f, max);
        float ratio1 = (double) max > 0.0 ? num2 / max : 0.0f;
        this.m_sb.Clear();
        this.m_sb.Append("[");
        Vector2 vector2_1 = MyGuiManager.MeasureStringRaw(this.m_fontId, this.m_sb, this.m_decorationSize.Y / MyGuiManager.MeasureStringRaw(this.m_fontId, this.m_sb, 1f).Y);
        this.m_sb.Clear();
        this.m_sb.Append(string.Format("{0:0}", (object) (float) ((double) ratio1 * 100.0)));
        Vector2 vector2_2 = MyGuiManager.MeasureStringRaw(this.m_fontId, this.m_sb, this.m_fontScale);
        MySprite sprite1 = new MySprite()
        {
          Position = new Vector2?(new Vector2(this.m_halfSize.X + x * 0.6f - num1, this.m_firstLine - vector2_2.Y * 0.5f)),
          Size = new Vector2?(new Vector2(this.m_innerSize.X, this.m_innerSize.Y)),
          Type = SpriteType.TEXT,
          FontId = this.m_fontId,
          Alignment = TextAlignment.LEFT,
          Color = new Color?(this.m_foregroundColor),
          RotationOrScale = this.m_fontScale,
          Data = this.m_sb.ToString()
        };
        frame.Add(sprite1);
        MySprite sprite2 = new MySprite(SpriteType.TEXTURE, MyTSSEnergyHydrogen.ENERGY_ICON, new Vector2?(), new Vector2?(), new Color?(this.m_foregroundColor), (string) null, TextAlignment.CENTER, 0.0f)
        {
          Position = new Vector2?(new Vector2(this.m_halfSize.X - x * 0.6f - num1, this.m_firstLine)),
          Size = new Vector2?(new Vector2(vector2_1.Y * 0.6f))
        };
        frame.Add(sprite2);
        this.AddProgressBar(frame, new Vector2(this.m_halfSize.X - num1, this.m_firstLine), new Vector2(x, vector2_1.Y * 0.4f), ratio1, barBgColor, this.m_foregroundColor, (string) null, (string) null);
        float num3 = 0.0f;
        foreach (Sandbox.Game.Entities.Interfaces.IMyGasTank tankBlock in this.m_tankBlocks)
          num3 += (float) tankBlock.FilledRatio * tankBlock.GasCapacity;
        float ratio2 = (double) this.m_maxHydrogen > 0.0 ? num3 / this.m_maxHydrogen : 0.0f;
        this.m_sb.Clear();
        this.m_sb.Append(string.Format("{0:0}", (object) (float) ((double) ratio2 * 100.0)));
        Vector2 vector2_3 = MyGuiManager.MeasureStringRaw(this.m_fontId, this.m_sb, this.m_fontScale);
        MySprite mySprite = new MySprite();
        mySprite.Position = new Vector2?(new Vector2(this.m_halfSize.X + x * 0.6f - num1, this.m_secondLine - vector2_3.Y * 0.5f));
        mySprite.Size = new Vector2?(new Vector2(this.m_innerSize.X, this.m_innerSize.Y));
        mySprite.Type = SpriteType.TEXT;
        mySprite.FontId = this.m_fontId;
        mySprite.Alignment = TextAlignment.LEFT;
        mySprite.Color = new Color?(this.m_foregroundColor);
        mySprite.RotationOrScale = this.m_fontScale;
        mySprite.Data = this.m_sb.ToString();
        MySprite sprite3 = mySprite;
        frame.Add(sprite3);
        mySprite = new MySprite(SpriteType.TEXTURE, MyTSSEnergyHydrogen.HYDROGEN_ICON, new Vector2?(), new Vector2?(), new Color?(this.m_foregroundColor), (string) null, TextAlignment.CENTER, 0.0f);
        mySprite.Position = new Vector2?(new Vector2(this.m_halfSize.X - x * 0.6f - num1, this.m_secondLine));
        mySprite.Size = new Vector2?(new Vector2(vector2_1.Y * 0.6f));
        MySprite sprite4 = mySprite;
        frame.Add(sprite4);
        this.AddProgressBar(frame, new Vector2(this.m_halfSize.X - num1, this.m_secondLine), new Vector2(x, vector2_1.Y * 0.4f), ratio2, barBgColor, this.m_foregroundColor, (string) null, (string) null);
        float scale = (float) ((double) this.m_innerSize.Y / 256.0 * 0.899999976158142);
        this.AddBrackets(frame, new Vector2(64f, 256f), scale);
      }
    }

    public override void Dispose()
    {
      base.Dispose();
    }

    private void Recalculate()
    {
      this.m_maxHydrogen = 0.0f;
      if (this.m_grid == null)
        return;
      foreach (IMyConveyorEndpointBlock conveyorEndpointBlock in this.m_grid.GridSystems.ConveyorSystem.ConveyorEndpointBlocks)
      {
        Sandbox.Game.Entities.Interfaces.IMyGasTank myGasTank = conveyorEndpointBlock as Sandbox.Game.Entities.Interfaces.IMyGasTank;
        if (myGasTank != null && myGasTank.IsResourceStorage(MyResourceDistributorComponent.HydrogenId))
        {
          this.m_maxHydrogen += myGasTank.GasCapacity;
          this.m_tankBlocks.Add(myGasTank);
        }
      }
    }

    private void ConveyorSystemOnBlockRemoved(MyCubeBlock myCubeBlock)
    {
      Sandbox.Game.Entities.Interfaces.IMyGasTank myGasTank = myCubeBlock as Sandbox.Game.Entities.Interfaces.IMyGasTank;
      if (myGasTank == null || !myGasTank.IsResourceStorage(MyResourceDistributorComponent.HydrogenId))
        return;
      this.m_maxHydrogen -= myGasTank.GasCapacity;
      this.m_tankBlocks.Remove(myGasTank);
    }

    private void ConveyorSystemOnBlockAdded(MyCubeBlock myCubeBlock)
    {
      Sandbox.Game.Entities.Interfaces.IMyGasTank myGasTank = myCubeBlock as Sandbox.Game.Entities.Interfaces.IMyGasTank;
      if (myGasTank == null || !myGasTank.IsResourceStorage(MyResourceDistributorComponent.HydrogenId))
        return;
      this.m_maxHydrogen += myGasTank.GasCapacity;
      this.m_tankBlocks.Add(myGasTank);
    }
  }
}
