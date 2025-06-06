/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated January 1, 2020. Replaces all prior versions.
 *
 * Copyright (c) 2013-2020, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

namespace Spine
{
    /// <summary>Stores the setup pose for an IkConstraint.</summary>
    public class IkConstraintData : ConstraintData
    {
        internal ExposedList<BoneData> bones = new ExposedList<BoneData>();
        internal BoneData target;
        internal int bendDirection = 1;
        internal bool compress, stretch, uniform;
        internal float mix = 1, softness;

        public IkConstraintData(string name) : base(name)
        {
        }

        /// <summary>The bones that are constrained by this IK Constraint.</summary>
        public ExposedList<BoneData> Bones
        {
            get { return this.bones; }
        }

        /// <summary>The bone that is the IK target.</summary>
        public BoneData Target
        {
            get { return this.target; }
            set { this.target = value; }
        }

        /// <summary>
        /// A percentage (0-1) that controls the mix between the constraint and unconstrained rotations.</summary>
        public float Mix
        {
            get { return this.mix; }
            set { this.mix = value; }
        }

        ///<summary>For two bone IK, the distance from the maximum reach of the bones that rotation will slow.</summary>
        public float Softness
        {
            get { return this.softness; }
            set { this.softness = value; }
        }

        /// <summary>Controls the bend direction of the IK bones, either 1 or -1.</summary>
        public int BendDirection
        {
            get { return this.bendDirection; }
            set { this.bendDirection = value; }
        }

        /// <summary>
        /// When true, and only a single bone is being constrained,
        /// if the target is too close, the bone is scaled to reach it. </summary>
        public bool Compress
        {
            get { return this.compress; }
            set { this.compress = value; }
        }

        /// <summary>
        /// When true, if the target is out of range, the parent bone is scaled on the X axis to reach it.
        /// If the bone has local nonuniform scale, stretching is not applied.</summary>
        public bool Stretch
        {
            get { return this.stretch; }
            set { this.stretch = value; }
        }

        /// <summary>
        /// When true, only a single bone is being constrained and Compress or Stretch is used,
        /// the bone is scaled both on the X and Y axes.</summary>
        public bool Uniform
        {
            get { return this.uniform; }
            set { this.uniform = value; }
        }
    }
}
