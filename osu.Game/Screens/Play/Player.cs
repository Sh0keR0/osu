﻿//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Platform;
using osu.Framework.Timing;
using osu.Game.Database;
using osu.Game.Modes;
using osu.Game.Modes.Objects;
using osu.Game.Modes.Objects.Drawables;
using osu.Game.Screens.Backgrounds;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;
using OpenTK;
using osu.Framework.GameModes;
using osu.Game.Modes.UI;
using osu.Game.Screens.Ranking;

namespace osu.Game.Screens.Play
{
    public class Player : OsuGameMode
    {
        public bool Autoplay;

        protected override BackgroundMode CreateBackground() => null;

        internal override bool ShowOverlays => false;

        public BeatmapInfo BeatmapInfo;

        public PlayMode PreferredPlayMode;
        
        private IAdjustableClock sourceClock;

        private Ruleset ruleset;

        private ScoreProcessor scoreProcessor;
        private HitRenderer hitRenderer;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, BeatmapDatabase beatmaps, OsuGameBase game)
        {
            try
            {
                if (Beatmap == null)
                    Beatmap = beatmaps.GetWorkingBeatmap(BeatmapInfo);
            }
            catch
            {
                //couldn't load, hard abort!
                Exit();
                return;
            }

            AudioTrack track = Beatmap.Track;

            if (track != null)
            {
                audio.Track.SetExclusive(track);
                sourceClock = track;
            }

            sourceClock = (IAdjustableClock)track ?? new StopwatchClock();
            Clock = new InterpolatingFramedClock(sourceClock);

            Schedule(() =>
            {
                sourceClock.Reset();
                sourceClock.Start();
            });

            var beatmap = Beatmap.Beatmap;

            if (beatmap.BeatmapInfo?.Mode > PlayMode.Osu)
            {
                //we only support osu! mode for now because the hitobject parsing is crappy and needs a refactor.
                Exit();
                return;
            }

            PlayMode usablePlayMode = beatmap.BeatmapInfo?.Mode > PlayMode.Osu ? beatmap.BeatmapInfo.Mode : PreferredPlayMode;

            ruleset = Ruleset.GetRuleset(usablePlayMode);

            var scoreOverlay = ruleset.CreateScoreOverlay();
            scoreOverlay.BindProcessor(scoreProcessor = ruleset.CreateScoreProcessor());

            hitRenderer = ruleset.CreateHitRendererWith(beatmap.HitObjects);

            hitRenderer.OnJudgement += scoreProcessor.AddJudgement;
            hitRenderer.OnAllJudged += hitRenderer_OnAllJudged;

            if (Autoplay)
                hitRenderer.Schedule(() => hitRenderer.DrawableObjects.ForEach(h => h.State = ArmedState.Hit));

            Children = new Drawable[]
            {
                new PlayerInputManager(game.Host)
                {
                    PassThrough = false,
                    Children = new Drawable[]
                    {
                        hitRenderer,
                    }
                },
                scoreOverlay,
            };
        }

        private void hitRenderer_OnAllJudged()
        {
            Delay(1000);
            Schedule(delegate
            {
                Push(new Results
                {
                    Score = scoreProcessor.GetScore()
                });
            });
        }

        protected override void OnEntering(GameMode last)
        {
            base.OnEntering(last);

            (Background as BackgroundModeBeatmap)?.BlurTo(Vector2.Zero, 1000);
        }

        protected override void Update()
        {
            base.Update();
            Clock.ProcessFrame();
        }

        class PlayerInputManager : UserInputManager
        {
            public PlayerInputManager(BasicGameHost host)
                : base(host)
            {
            }

            bool leftViaKeyboard;
            bool rightViaKeyboard;

            protected override void TransformState(InputState state)
            {
                base.TransformState(state);

                MouseState mouse = (MouseState)state.Mouse;

                if (state.Keyboard != null)
                {
                    leftViaKeyboard = state.Keyboard.Keys.Contains(Key.Z);
                    rightViaKeyboard = state.Keyboard.Keys.Contains(Key.X);
                }

                if (state.Mouse != null)
                {
                    if (leftViaKeyboard) mouse.ButtonStates.Find(s => s.Button == MouseButton.Left).State = true;
                    if (rightViaKeyboard) mouse.ButtonStates.Find(s => s.Button == MouseButton.Right).State = true;
                }

            }
        }
    }
}