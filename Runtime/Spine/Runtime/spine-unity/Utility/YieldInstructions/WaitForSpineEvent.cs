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

using System.Collections;
using UnityEngine;

namespace Spine.Unity
{
    /// <summary>
    /// Use this as a condition-blocking yield instruction for Unity Coroutines.
    /// The routine will pause until the AnimationState fires an event matching the given event name or EventData reference.</summary>
    public class WaitForSpineEvent : IEnumerator
    {

        private Spine.EventData m_TargetEvent;
        private string m_EventName;
        private Spine.AnimationState m_AnimationState;

        private bool m_WasFired = false;
        private bool m_unsubscribeAfterFiring = false;

        #region Constructors
        private void Subscribe(Spine.AnimationState state, Spine.EventData eventDataReference, bool unsubscribe)
        {
            if (state == null)
            {
                Debug.LogWarning("AnimationState argument was null. Coroutine will continue immediately.");
                this.m_WasFired = true;
                return;
            }
            else if (eventDataReference == null)
            {
                Debug.LogWarning("eventDataReference argument was null. Coroutine will continue immediately.");
                this.m_WasFired = true;
                return;
            }

            this.m_AnimationState = state;
            this.m_TargetEvent = eventDataReference;
            state.Event += this.HandleAnimationStateEvent;

            this.m_unsubscribeAfterFiring = unsubscribe;

        }

        private void SubscribeByName(Spine.AnimationState state, string eventName, bool unsubscribe)
        {
            if (state == null)
            {
                Debug.LogWarning("AnimationState argument was null. Coroutine will continue immediately.");
                this.m_WasFired = true;
                return;
            }
            else if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("eventName argument was null. Coroutine will continue immediately.");
                this.m_WasFired = true;
                return;
            }

            this.m_AnimationState = state;
            this.m_EventName = eventName;
            state.Event += this.HandleAnimationStateEventByName;

            this.m_unsubscribeAfterFiring = unsubscribe;
        }

        public WaitForSpineEvent(Spine.AnimationState state, Spine.EventData eventDataReference, bool unsubscribeAfterFiring = true)
        {
            this.Subscribe(state, eventDataReference, unsubscribeAfterFiring);
        }

        public WaitForSpineEvent(SkeletonAnimation skeletonAnimation, Spine.EventData eventDataReference, bool unsubscribeAfterFiring = true)
        {
            // If skeletonAnimation is invalid, its state will be null. Subscribe handles null states just fine.
            this.Subscribe(skeletonAnimation.state, eventDataReference, unsubscribeAfterFiring);
        }

        public WaitForSpineEvent(Spine.AnimationState state, string eventName, bool unsubscribeAfterFiring = true)
        {
            this.SubscribeByName(state, eventName, unsubscribeAfterFiring);
        }

        public WaitForSpineEvent(SkeletonAnimation skeletonAnimation, string eventName, bool unsubscribeAfterFiring = true)
        {
            // If skeletonAnimation is invalid, its state will be null. Subscribe handles null states just fine.
            this.SubscribeByName(skeletonAnimation.state, eventName, unsubscribeAfterFiring);
        }
        #endregion

        #region Event Handlers
        private void HandleAnimationStateEventByName(Spine.TrackEntry trackEntry, Spine.Event e)
        {
            this.m_WasFired |= (e.Data.Name == this.m_EventName);           // Check event name string match.
            if (this.m_WasFired && this.m_unsubscribeAfterFiring)
                this.m_AnimationState.Event -= this.HandleAnimationStateEventByName;    // Unsubscribe after correct event fires.
        }

        private void HandleAnimationStateEvent(Spine.TrackEntry trackEntry, Spine.Event e)
        {
            this.m_WasFired |= (e.Data == this.m_TargetEvent);          // Check event data reference match.
            if (this.m_WasFired && this.m_unsubscribeAfterFiring)
                this.m_AnimationState.Event -= this.HandleAnimationStateEvent;      // Usubscribe after correct event fires.
        }
        #endregion

        #region Reuse
        /// <summary>
        /// By default, WaitForSpineEvent will unsubscribe from the event immediately after it fires a correct matching event.
        /// If you want to reuse this WaitForSpineEvent instance on the same event, you can set this to false.</summary>
        public bool WillUnsubscribeAfterFiring { get { return this.m_unsubscribeAfterFiring; } set { this.m_unsubscribeAfterFiring = value; } }

        public WaitForSpineEvent NowWaitFor(Spine.AnimationState state, Spine.EventData eventDataReference, bool unsubscribeAfterFiring = true)
        {
            ((IEnumerator)this).Reset();
            this.Clear(state);
            this.Subscribe(state, eventDataReference, unsubscribeAfterFiring);

            return this;
        }

        public WaitForSpineEvent NowWaitFor(Spine.AnimationState state, string eventName, bool unsubscribeAfterFiring = true)
        {
            ((IEnumerator)this).Reset();
            this.Clear(state);
            this.SubscribeByName(state, eventName, unsubscribeAfterFiring);

            return this;
        }

        private void Clear(Spine.AnimationState state)
        {
            state.Event -= this.HandleAnimationStateEvent;
            state.Event -= this.HandleAnimationStateEventByName;
        }
        #endregion

        #region IEnumerator
        bool IEnumerator.MoveNext()
        {
            if (this.m_WasFired)
            {
                ((IEnumerator)this).Reset();    // auto-reset for YieldInstruction reuse
                return false;
            }

            return true;
        }
        void IEnumerator.Reset() { this.m_WasFired = false; }
        object IEnumerator.Current { get { return null; } }
        #endregion
    }
}
