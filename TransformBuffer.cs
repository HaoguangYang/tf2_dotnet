/* Copyright 2022 Stefan Hoffmann <stefan.hoffmann@schiller.de>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Numerics;
using builtin_interfaces.msg;
using geometry_msgs.msg;

namespace ROS2.Tf2DotNet
{
    // Using "Buffer" as type name like in c++ conflicts with "System.Buffer",
    // so using "TransformBuffer" instead.
    //
    // Make sure to implement the Dispose Pattern when unsealing this class.
    public sealed class TransformBuffer : IDisposable
    {
        private readonly SafeBufferCoreHandle _handle;
        private bool _disposed = false;

        public TransformBuffer()
        {
            Tf2ExceptionHelper.ResetMessage();
            _handle = Interop.tf2_dotnet_native_buffer_core_create(out Tf2ExceptionType exceptionType, Tf2ExceptionHelper.MessageBuffer);
            Tf2ExceptionHelper.ThrowIfHasException(exceptionType);
        }

        public void Dispose()
        {
            _handle.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Add transform information to the tf data structure.
        /// </summary>
        /// <param name="transform">The transform to store.</param>
        /// <param name="authority">The source of the information for this transform.</param>
        /// <param name="isStatic">Record this transform as a static transform. It will be good across all time. (This cannot be changed after the first call.)</param>
        /// <returns>True unless an error occurred.</returns>
        public bool SetTransform(TransformStamped transform, string authority, bool isStatic = false)
        {
            ThrowIfDisposed();

            Tf2ExceptionHelper.ResetMessage();

            int result = Interop.tf2_dotnet_native_buffer_core_set_transform(
                _handle,
                transform.Header.Stamp.Sec,
                transform.Header.Stamp.Nanosec,
                transform.Header.Frame_id,
                transform.Child_frame_id,
                transform.Transform.Translation.X,
                transform.Transform.Translation.Y,
                transform.Transform.Translation.Z,
                transform.Transform.Rotation.X,
                transform.Transform.Rotation.Y,
                transform.Transform.Rotation.Z,
                transform.Transform.Rotation.W,
                authority,
                isStatic ? 1 : 0,
                out Tf2ExceptionType exceptionType,
                Tf2ExceptionHelper.MessageBuffer);

            Tf2ExceptionHelper.ThrowIfHasException(exceptionType);
            
            return result == 1;
        }

        /// <summary>
        /// Get the transform between two frames by frame ID.
        /// </summary>
        /// <param name="targetFrame">The frame to which data should be transformed.</param>
        /// <param name="sourceFrame">The frame where the data originated.</param>
        /// <param name="time">The time at which the value of the transform is desired. (<c>null</c> will get the latest)</param>
        /// <returns>The transform between the frames.</returns>
        /// <exception cref="LookupException"></exception>
        /// <exception cref="ConnectivityException"></exception>
        /// <exception cref="ExtrapolationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public TransformStamped LookupTransform(
            string targetFrame,
            string sourceFrame,
            Time time = null)
        {
            ThrowIfDisposed();

            int sec;
            uint nanosec;
            if (time != null)
            {
                sec = time.Sec;
                nanosec = time.Nanosec;
            }
            else
            {
                sec = 0;
                nanosec = 0;
            }

            Tf2ExceptionHelper.ResetMessage();

            Transform transform = Interop.tf2_dotnet_native_buffer_core_lookup_transform(
                _handle,
                targetFrame,
                sourceFrame,
                sec,
                nanosec,
                out Tf2ExceptionType exceptionType,
                Tf2ExceptionHelper.MessageBuffer);

            Tf2ExceptionHelper.ThrowIfHasException(exceptionType);

            TransformStamped transformStamped = transform.ToTransformStamped(targetFrame, sourceFrame);
            return transformStamped;
        }

        /// <summary>
        /// Get the transform between two frames by frame ID assuming fixed frame.
        /// </summary>
        /// <param name="targetFrame">The frame to which data should be transformed.</param>
        /// <param name="targetTime">The time to which the data should be transformed. (<c>null</c> will get the latest)</param>
        /// <param name="sourceFrame">The frame where the data originated.</param>
        /// <param name="sourceTime">The time at which the source_frame should be evaluated. (<c>null</c> will get the latest)</param>
        /// <param name="fixedFrame">The frame in which to assume the transform is constant in time.</param>
        /// <returns>The transform between the frames.</returns>
        /// <exception cref="LookupException"></exception>
        /// <exception cref="ConnectivityException"></exception>
        /// <exception cref="ExtrapolationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public TransformStamped LookupTransform(
            string targetFrame,
            Time targetTime,
            string sourceFrame,
            Time sourceTime,
            string fixedFrame)
        {
            ThrowIfDisposed();

            int targetSec;
            uint targetNanosec;
            if (targetTime != null)
            {
                targetSec = targetTime.Sec;
                targetNanosec = targetTime.Nanosec;
            }
            else
            {
                targetSec = 0;
                targetNanosec = 0;
            }

            int sourceSec;
            uint sourceNanosec;
            if (sourceTime != null)
            {
                sourceSec = sourceTime.Sec;
                sourceNanosec = sourceTime.Nanosec;
            }
            else
            {
                sourceSec = 0;
                sourceNanosec = 0;
            }

            Tf2ExceptionHelper.ResetMessage();

            Transform transform = Interop.tf2_dotnet_native_buffer_core_lookup_transform_full(
                _handle,
                targetFrame,
                targetSec,
                targetNanosec,
                sourceFrame,
                sourceSec,
                sourceNanosec,
                fixedFrame,
                out Tf2ExceptionType exceptionType,
                Tf2ExceptionHelper.MessageBuffer);

            Tf2ExceptionHelper.ThrowIfHasException(exceptionType);

            TransformStamped transformStamped = transform.ToTransformStamped(targetFrame, sourceFrame);
            return transformStamped;
        }

        /// <summary>
        /// Test if a transform is possible.
        /// </summary>
        /// <param name="targetFrame">The frame into which to transform.</param>
        /// <param name="sourceFrame">The frame from which to transform.</param>
        /// <param name="time">The time at which to transform.</param>
        /// <param name="errorMessage">The error message why the transform failed.</param>
        /// <returns>True if the transform is possible, false otherwise.</returns>
        public bool CanTransform(
            string targetFrame,
            string sourceFrame,
            Time time,
            out string errorMessage)
        {
            ThrowIfDisposed();

            var errorMessageBuffer = new byte[Tf2ExceptionHelper.MessageBufferLength];

            bool result = CanTransformInner(
                targetFrame,
                sourceFrame,
                time,
                errorMessageBuffer);
            
            errorMessage = System.Text.Encoding.UTF8.GetString(errorMessageBuffer).TrimEnd('\0');

            return result;
        }

        /// <summary>
        /// Test if a transform is possible.
        /// </summary>
        /// <param name="targetFrame">The frame into which to transform.</param>
        /// <param name="sourceFrame">The frame from which to transform.</param>
        /// <param name="time">The time at which to transform.</param>
        /// <returns>True if the transform is possible, false otherwise.</returns>
        public bool CanTransform(
            string targetFrame,
            string sourceFrame,
            Time time = null)
        {
            ThrowIfDisposed();

            byte[] errorMessageBuffer = null;

            bool result = CanTransformInner(
                targetFrame,
                sourceFrame,
                time,
                errorMessageBuffer);
            
            return result;
        }

        private bool CanTransformInner(
            string targetFrame,
            string sourceFrame,
            Time time,
            byte[] errorMessageBuffer)
        {
            int sec;
            uint nanosec;
            if (time != null)
            {
                sec = time.Sec;
                nanosec = time.Nanosec;
            }
            else
            {
                sec = 0;
                nanosec = 0;
            }

            Tf2ExceptionHelper.ResetMessage();

            int result = Interop.tf2_dotnet_native_buffer_core_can_transform(
                _handle,
                targetFrame,
                sourceFrame,
                sec,
                nanosec,
                errorMessageBuffer,
                out Tf2ExceptionType exceptionType,
                Tf2ExceptionHelper.MessageBuffer);

            Tf2ExceptionHelper.ThrowIfHasException(exceptionType);

            return result == 1;
        }

        /// <summary>
        /// Test if a transform is possible.
        /// </summary>
        /// <param name="targetFrame">The frame into which to transform.</param>
        /// <param name="targetTime">The time into which to transform.</param>
        /// <param name="sourceFrame">The frame from which to transform.</param>
        /// <param name="sourceTime">The time from which to transform.</param>
        /// <param name="fixedFrame">The frame in which to treat the transform as constant in time.</param>
        /// <param name="errorMessage">The error message why the transform failed.</param>
        /// <returns>True if the transform is possible, false otherwise.</returns>
        public bool CanTransform(
            string targetFrame,
            Time targetTime,
            string sourceFrame,
            Time sourceTime,
            string fixedFrame,
            out string errorMessage)
        {
            ThrowIfDisposed();

            var errorMessageBuffer = new byte[Tf2ExceptionHelper.MessageBufferLength];

            bool result = CanTransformInner(
                targetFrame,
                targetTime,
                sourceFrame,
                sourceTime,
                fixedFrame,
                errorMessageBuffer);
            
            errorMessage = System.Text.Encoding.UTF8.GetString(errorMessageBuffer).TrimEnd('\0');

            return result;
        }

        
        /// <summary>
        /// Test if a transform is possible.
        /// </summary>
        /// <param name="targetFrame">The frame into which to transform.</param>
        /// <param name="targetTime">The time into which to transform.</param>
        /// <param name="sourceFrame">The frame from which to transform.</param>
        /// <param name="sourceTime">The time from which to transform.</param>
        /// <param name="fixedFrame">The frame in which to treat the transform as constant in time.</param>
        /// <returns>True if the transform is possible, false otherwise.</returns>
        public bool CanTransform(
            string targetFrame,
            Time targetTime,
            string sourceFrame,
            Time sourceTime,
            string fixedFrame)
        {
            ThrowIfDisposed();

            byte[] errorMessageBuffer = null;

            bool result = CanTransformInner(
                targetFrame,
                targetTime,
                sourceFrame,
                sourceTime,
                fixedFrame,
                errorMessageBuffer);

            return result;
        }
        
        private bool CanTransformInner(
            string targetFrame,
            Time targetTime,
            string sourceFrame,
            Time sourceTime,
            string fixedFrame,
            byte[] errorMessageBuffer)
        {
            int targetSec;
            uint targetNanosec;
            if (targetTime != null)
            {
                targetSec = targetTime.Sec;
                targetNanosec = targetTime.Nanosec;
            }
            else
            {
                targetSec = 0;
                targetNanosec = 0;
            }

            int sourceSec;
            uint sourceNanosec;
            if (sourceTime != null)
            {
                sourceSec = sourceTime.Sec;
                sourceNanosec = sourceTime.Nanosec;
            }
            else
            {
                sourceSec = 0;
                sourceNanosec = 0;
            }
            
            Tf2ExceptionHelper.ResetMessage();

            int result = Interop.tf2_dotnet_native_buffer_core_can_transform_full(
                _handle,
                targetFrame,
                targetSec,
                targetNanosec,
                sourceFrame,
                sourceSec,
                sourceNanosec,
                fixedFrame,
                errorMessageBuffer,
                out Tf2ExceptionType exceptionType,
                Tf2ExceptionHelper.MessageBuffer);

            Tf2ExceptionHelper.ThrowIfHasException(exceptionType);

            return result == 1;
        }

        /// <summary>
        /// transforms a vector
        /// </summary>
        /// <param name="position">the vector to be transfomed</param>
        /// <param name="transform">The transform with which we will transform the vector</param>
        /// <returns>the transformed vector</returns>
        public System.Numerics.Vector3 Transform(System.Numerics.Vector3 position, geometry_msgs.msg.Transform transform) {
            // convert this transform's rotation into a System.Numerics.Quaternion for the rotation
            System.Numerics.Quaternion rotator = new System.Numerics.Quaternion((float) transform.Rotation.X,(float) transform.Rotation.Y,(float) transform.Rotation.Z,(float) transform.Rotation.W);

            // translate
            System.Numerics.Vector3 translated = new System.Numerics.Vector3((float) (position.X + transform.Translation.X),(float) (position.Y + transform.Translation.Y), (float) (position.Z + transform.Translation.Z));

            // rotate
            return System.Numerics.Vector3.Transform(translated,rotator);
        }

        /// <summary>
        /// transforms a vector
        /// </summary>
        /// <param name="position">the vector to be transfomed</param>
        /// <param name="transform">The transform with which we will transform the vector</param>
        /// <returns>the transformed vector</returns>
        public System.Numerics.Vector3 Transform(System.Numerics.Vector3 position, geometry_msgs.msg.TransformStamped transform) {
            return Transform(position, transform.Transform);
        }

        /// <summary>
        /// transforms a Point as if it were a vector
        /// </summary>
        /// <remarks>
        /// Note that the returned point only has floating point precision. Double precision is lost!
        /// </remarks>
        /// 
        /// <param name="point">the point to be transfomed</param>
        /// <param name="transform">The transform with which we will transform the vector</param>
        /// <returns>the transformed vector</returns>
        public geometry_msgs.msg.Point Transform(geometry_msgs.msg.Point point, geometry_msgs.msg.TransformStamped transform) {
            System.Numerics.Vector3 transVec = Transform(new System.Numerics.Vector3((float) point.X,(float)  point.Y,(float)  point.Z), transform);
            geometry_msgs.msg.Point transPoint = new geometry_msgs.msg.Point();
            transPoint.X = transVec.X;
            transPoint.Y = transVec.Y;
            transPoint.Z = transVec.Z;
            return transPoint;
        }

  
        /// <summary>
        /// returns a transform that transforms from the start vector to the end vector, in the plane orthogonal to both vectors
        /// </summary>
        /// <remarks>
        /// This method only uses float precision
        /// </remarks>
        public geometry_msgs.msg.Transform TransformFromVectors(Vector3 start, Vector3 end) {
            
            System.Numerics.Vector3 crossProduct = System.Numerics.Vector3.Cross(start,end);


            float angle = crossProduct.Length;

            crossProduct /= crossProduct.Length; // convert to unit vector for quaternion

            crossProduct *= Math.Sin(angle/2) * crossProduct; // calculate imaginary components of quaternion

            // create quaternion which is rotation of magnitude <angle> about the axis defined by <crossProduct>
            System.Numerics.Quaternion q = new System.Numerics.Quaternion(Math.Cos(angle/2), crossProduct.X, crossProduct.Y, crossProduct.Z);

            var msg = new geometry_msgs.msg.Transform();
            msg.Translation.X = translation.X;
            msg.Translation.Y = translation.Y;


            msg.Rotation.X = q.X;
            msg.Rotation.Y = q.Y;
            msg.Rotation.Z = q.Z;
            msg.Rotation.W = q.W;

            return msg;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
