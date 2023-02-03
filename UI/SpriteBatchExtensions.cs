using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Cornifer.UI
{
    public static class SpriteBatchExtensions
    {
        static Func<SpriteBatch, SpriteBatchState>? SpriteBatchStateGetter;
        static Func<SpriteBatch, bool>? SpriteBatchBeginCalledGetter;
        static Stack<SpriteBatchState> SpriteBatchStates = new();

        public static SpriteBatchState GetState(this SpriteBatch spriteBatch)
        {
            if (SpriteBatchStateGetter is null)
            {
                DynamicMethod dm = new("SpriteBatch_GetState", typeof(SpriteBatchState), new Type[] { typeof(SpriteBatch) });
                ILGenerator il = dm.GetILGenerator();

                (string, string)[] map = new[]
                {
                    ("_sortMode", "SpriteSortMode"),
                    ("_blendState", "BlendState"),
                    ("_samplerState", "SamplerState"),
                    ("_depthStencilState", "DepthStencilState"),
                    ("_rasterizerState", "RasterizerState"),
                    ("_effect", "Effect"),
                };

                int state = il.DeclareLocal(typeof(SpriteBatchState)).LocalIndex;

                il.Emit(OpCodes.Ldloca, state);
                il.Emit(OpCodes.Initobj, typeof(SpriteBatchState));

                foreach (var (sbField, stateField) in map)
                {
                    il.Emit(OpCodes.Ldloca, state);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, typeof(SpriteBatch).GetField(sbField, BindingFlags.NonPublic | BindingFlags.Instance)!);
                    il.Emit(OpCodes.Stfld, typeof(SpriteBatchState).GetField(stateField)!);
                }

                il.Emit(OpCodes.Ldloca, state);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, typeof(SpriteBatch).GetField("_spriteEffect", BindingFlags.NonPublic | BindingFlags.Instance)!);
                il.Emit(OpCodes.Call, typeof(SpriteEffect).GetProperty("TransformMatrix")!.GetGetMethod()!);
                il.Emit(OpCodes.Stfld, typeof(SpriteBatchState).GetField("TransformMatrix")!);

                il.Emit(OpCodes.Ldloc, state);
                il.Emit(OpCodes.Ret);

                SpriteBatchStateGetter = dm.CreateDelegate<Func<SpriteBatch, SpriteBatchState>>();
            }
            return SpriteBatchStateGetter(spriteBatch);
        }
        public static bool IsBeginCalled(this SpriteBatch spriteBatch)
        {
            if (SpriteBatchBeginCalledGetter is null)
            {
                DynamicMethod dm = new("SpriteBatch_GetBeginCalled", typeof(bool), new Type[] { typeof(SpriteBatch) });
                ILGenerator il = dm.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, typeof(SpriteBatch).GetField("_beginCalled", BindingFlags.NonPublic | BindingFlags.Instance)!);
                il.Emit(OpCodes.Ret);

                SpriteBatchBeginCalledGetter = dm.CreateDelegate<Func<SpriteBatch, bool>>();
            }
            return SpriteBatchBeginCalledGetter(spriteBatch);
        }

        public static void PushState(this SpriteBatch spriteBatch)
        {
            SpriteBatchStates.Push(spriteBatch.GetState());
        }
        public static void RestoreState(this SpriteBatch spriteBatch)
        {
            if (spriteBatch.IsBeginCalled())
                spriteBatch.End();
            spriteBatch.Begin(SpriteBatchStates.Pop());
        }

        public static void ChangeState(this SpriteBatch spriteBatch,
            Optional<SpriteSortMode> sortMode = default,
            Optional<BlendState> blendState = default,
            Optional<SamplerState> samplerState = default,
            Optional<DepthStencilState> depthStencilState = default,
            Optional<RasterizerState> rasterizerState = default,
            Optional<Effect> effect = default,
            Optional<Matrix?> transformMatrix = default)
        {
            SpriteBatchState state = spriteBatch.GetState();

            if (sortMode.HasValue) state.SpriteSortMode = sortMode.Value;
            if (blendState.HasValue) state.BlendState = blendState.Value;
            if (samplerState.HasValue) state.SamplerState = samplerState.Value;
            if (depthStencilState.HasValue) state.DepthStencilState = depthStencilState.Value;
            if (rasterizerState.HasValue) state.RasterizerState = rasterizerState.Value;
            if (effect.HasValue) state.Effect = effect.Value;
            if (transformMatrix.HasValue) state.TransformMatrix = transformMatrix.Value;

            if (spriteBatch.IsBeginCalled())
                spriteBatch.End();
            spriteBatch.Begin(state);
        }

        public static void PushAndChangeState(this SpriteBatch spriteBatch,
            Optional<SpriteSortMode> sortMode = default,
            Optional<BlendState> blendState = default,
            Optional<SamplerState> samplerState = default,
            Optional<DepthStencilState> depthStencilState = default,
            Optional<RasterizerState> rasterizerState = default,
            Optional<Effect> effect = default,
            Optional<Matrix?> transformMatrix = default)
        {
            SpriteBatchState state = spriteBatch.GetState();
            SpriteBatchStates.Push(state);

            if (sortMode.HasValue) state.SpriteSortMode = sortMode.Value;
            if (blendState.HasValue) state.BlendState = blendState.Value;
            if (samplerState.HasValue) state.SamplerState = samplerState.Value;
            if (depthStencilState.HasValue) state.DepthStencilState = depthStencilState.Value;
            if (rasterizerState.HasValue) state.RasterizerState = rasterizerState.Value;
            if (effect.HasValue) state.Effect = effect.Value;
            if (transformMatrix.HasValue) state.TransformMatrix = transformMatrix.Value;

            if (spriteBatch.IsBeginCalled())
                spriteBatch.End();
            spriteBatch.Begin(state);
        }

        public static void Begin(this SpriteBatch spriteBatch, SpriteBatchState state)
        {
            spriteBatch.Begin(state.SpriteSortMode, state.BlendState, state.SamplerState, state.DepthStencilState, state.RasterizerState, state.Effect, state.TransformMatrix);
        }
    }
}
