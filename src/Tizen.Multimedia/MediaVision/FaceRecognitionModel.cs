/*
 * Copyright (c) 2016 Samsung Electronics Co., Ltd All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the License);
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an AS IS BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using InteropModel = Tizen.Multimedia.Interop.MediaVision.FaceRecognitionModel;

namespace Tizen.Multimedia
{
    /// <summary>
    /// Represents the face recognition model interface.
    /// </summary>
    public class FaceRecognitionModel : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceRecognitionModel"/> class.
        /// </summary>
        /// <exception cref="NotSupportedException">The feature is not supported.</exception>
        public FaceRecognitionModel()
        {
            InteropModel.Create(out _handle).Validate("Failed to create FaceRecognitionModel");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceRecognitionModel"/> class withe the specified path.
        /// </summary>
        /// <remarks>
        /// Models have been saved by <see cref="Save()"/> can be loaded.
        /// </remarks>
        /// <param name="modelPath">Path to the model to load.</param>
        /// <exception cref="ArgumentNullException"><paramref name="modelPath"/> is null.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="modelPath"/> is invalid.</exception>
        /// <exception cref="NotSupportedException">
        ///     The feature is not supported.\n
        ///     - or -\n
        ///     <paramref name="modelPath"/> is not supported format.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">No permission to access the specified file.</exception>
        /// <seealso cref="Save(string)"/>
        public FaceRecognitionModel(string modelPath)
        {
            if (modelPath == null)
            {
                throw new ArgumentNullException(nameof(modelPath));
            }

            InteropModel.Load(modelPath, out _handle).
                Validate("Failed to load FaceRecognitionModel from file");
        }

        ~FaceRecognitionModel()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets labels that had been learned by the model.
        /// </summary>
        public int[] Labels
        {
            get
            {
                IntPtr unmangedArray = IntPtr.Zero;
                try
                {
                    uint numOfLabels = 0;

                    InteropModel.QueryLabels(Handle, out unmangedArray, out numOfLabels).
                        Validate("Failed to retrieve face labels.");

                    int[] labels = new int[numOfLabels];
                    Marshal.Copy(unmangedArray, labels, 0, (int)numOfLabels);

                    return labels;
                }
                finally
                {
                    if (unmangedArray != IntPtr.Zero)
                    {
                        Interop.Libc.Free(unmangedArray);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the recognition model to the file.
        /// </summary>
        /// <param name="path">Path to the file to save the model.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="UnauthorizedAccessException">No permission to write to the specified path.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="FaceRecognitionModel"/> has already been disposed of.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory for <paramref name="path"/> does not exist.</exception>
        public void Save(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var ret = InteropModel.Save(path, Handle);

            if (ret == MediaVisionError.InvalidPath)
            {
                throw new DirectoryNotFoundException($"The directory for the path({path}) does not exist.");
            }

            ret.Validate("Failed to save recognition model to file");
        }

        private MediaVisionError InvokeAdd(MediaVisionSource source, int label, Rectangle? area)
        {
            if (area != null)
            {
                var rect = area.Value.ToMarshalable();
                return InteropModel.Add(source.Handle, Handle, ref rect, label);
            }

            return InteropModel.Add(source.Handle, Handle, IntPtr.Zero, label);
        }

        /// <summary>
        /// Adds face image example to be used for face recognition model learning.
        /// </summary>
        /// <param name="source">The <see cref="MediaVisionSource"/> that contains face image.</param>
        /// <param name="label">The label that identifies face for which example is adding.
        ///     Specify the same labels for the face images of a single person when calling this method.
        ///     Has to be unique for each face</param>
        /// <param name="area">The rectangular location of the face image at the source image.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="FaceRecognitionModel"/> has already been disposed of.\n
        ///     - or -\n
        ///     <paramref name="source"/> has already been dispose of.
        /// </exception>
        /// <seealso cref="Learn(FaceRecognitionConfiguration)"/>
        public void Add(MediaVisionSource source, int label)
        {
            if (source == null)
            {
                throw new ArgumentException("Invalid source");
            }

            InvokeAdd(source, label, null).Validate("Failed to add face example image");
        }

        /// <summary>
        /// Adds face image example to be used for face recognition model learning.
        /// </summary>
        /// <param name="source">The <see cref="MediaVisionSource"/> that contains face image.</param>
        /// <param name="label">The label that identifies face for which example is adding.
        ///     Specify the same labels for the face images of a single person when calling this method.
        ///     Has to be unique for each face</param>
        /// <param name="area">The rectangular region of the face image at the source image.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="FaceRecognitionModel"/> has already been disposed of.\n
        ///     - or -\n
        ///     <paramref name="source"/> has already been dispose of.
        /// </exception>
        /// <seealso cref="Learn(FaceRecognitionConfiguration)"/>
        public void Add(MediaVisionSource source, int label, Rectangle area)
        {
            if (source == null)
            {
                throw new ArgumentException("Invalid source");
            }

            InvokeAdd(source, label, area).Validate("Failed to add face example image");
        }

        /// <summary>
        /// Removes all face examples added with the specified label.
        /// </summary>
        /// <param name="label">The label that identifies face for which examples will be removed.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="FaceRecognitionModel"/> has already been disposed of.</exception>
        /// <returns>true if the examples are successfully removed; otherwise, false if there is no example labeled with the specified label.</returns>
        /// <seealso cref="Add(MediaVisionSource, int)"/>
        /// <seealso cref="Add(MediaVisionSource, int, Rectangle)"/>
        public bool Remove(int label)
        {
            var ret = InteropModel.Remove(Handle, ref label);

            if (ret == MediaVisionError.KeyNotAvailable)
            {
                return false;
            }

            ret.Validate("Failed to remove image example");
            return true;
        }

        /// <summary>
        /// Removes all face examples.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="FaceRecognitionModel"/> has already been disposed of.</exception>
        public void Reset()
        {
            InteropModel.Reset(Handle).Validate("Failed to reset image example");
        }


        /// <summary>
        /// Learns face recognition model.
        /// </summary>
        /// <remarks>
        /// Before you start learning process, face recognition models has to be filled with training data - face image examples.
        /// These examples has to be provided by <see cref="Add(MediaVisionSource, int)"/> or <see cref="Add(MediaVisionSource, int, Rectangle)"/>.
        /// Recognition accuracy is usually increased when the different examples of the identical face are added more and more.
        /// But it depends on the used learning algorithm.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="FaceRecognitionModel"/> has already been disposed of.</exception>
        /// <exception cref="InvalidOperationException">No examples added.</exception>
        /// <seealso cref="Add(MediaVisionSource, int)"/>
        /// <seealso cref="Add(MediaVisionSource, int, Rectangle)"/>
        public void Learn()
        {
            Learn(null);
        }

        /// <summary>
        /// Learns face recognition model with <see cref="FaceRecognitionConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// Before you start learning process, face recognition models has to be filled with training data - face image examples.
        /// These examples has to be provided by <see cref="Add(MediaVisionSource, int)"/> or <see cref="Add(MediaVisionSource, int, Rectangle)"/>.
        /// Recognition accuracy is usually increased when the different examples of the identical face are added more and more.
        /// But it depends on the used learning algorithm.
        /// </remarks>
        /// <param name="config">The configuration used for learning of the recognition models. This value can be null.</param>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="FaceRecognitionModel"/> has already been disposed of.\n
        ///     - or -\n
        ///     <paramref name="config"/> has already been disposed of.
        /// </exception>
        /// <exception cref="InvalidOperationException">No examples added.</exception>
        /// <seealso cref="Add(MediaVisionSource, int)"/>
        /// <seealso cref="Add(MediaVisionSource, int, Rectangle)"/>
        public void Learn(FaceRecognitionConfiguration config)
        {
            InteropModel.Learn(EngineConfiguration.GetHandle(config), Handle).
                Validate("Failed to learn");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            InteropModel.Destroy(_handle);
            _disposed = true;
        }

        internal IntPtr Handle
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(FaceRecognitionModel));
                }
                return _handle;
            }
        }
    }
}
