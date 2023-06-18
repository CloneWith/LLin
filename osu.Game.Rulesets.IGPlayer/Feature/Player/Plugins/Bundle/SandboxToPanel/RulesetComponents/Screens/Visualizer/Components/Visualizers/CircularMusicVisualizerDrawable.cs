﻿using osu.Framework.Bindables;
using osuTK;

namespace osu.Game.Rulesets.IGPlayer.Player.Plugins.Bundle.SandboxToPanel.RulesetComponents.Screens.Visualizer.Components.Visualizers
{
    public abstract partial class CircularMusicVisualizerDrawable : MusicVisualizerDrawable
    {
        public readonly Bindable<float> DegreeValue = new Bindable<float>();

        protected override VisualizerDrawNode CreateVisualizerDrawNode() => CreateCircularVisualizerDrawNode();

        protected abstract CircularVisualizerDrawNode CreateCircularVisualizerDrawNode();

        protected abstract partial class CircularVisualizerDrawNode : VisualizerDrawNode
        {
            protected new CircularMusicVisualizerDrawable Source => (CircularMusicVisualizerDrawable)base.Source;

            protected float DegreeValue;

            public CircularVisualizerDrawNode(CircularMusicVisualizerDrawable source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();
                DegreeValue = Source.DegreeValue.Value;
            }

            protected override float Spacing => DegreeValue / AudioData.Count;

            protected override Vector2 Inflation => DrawInfo.MatrixInverse.ExtractScale().Xy;
        }
    }
}