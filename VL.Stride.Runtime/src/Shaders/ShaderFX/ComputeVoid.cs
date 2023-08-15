using System;
using System.Reactive;
using System.Runtime.Serialization;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace VL.Stride.Shaders.ShaderFX
{
    public interface IComputeVoid : IComputeNode
    {

    }

    public abstract class ComputeVoid : ComputeNode<Unit>, IComputeVoid
    {
        public override bool HasChanged => false;
    }
}
