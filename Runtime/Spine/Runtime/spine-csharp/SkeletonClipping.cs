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

using System;

namespace Spine
{
    public class SkeletonClipping
    {
        internal readonly Triangulator triangulator = new Triangulator();
        internal readonly ExposedList<float> clippingPolygon = new ExposedList<float>();
        internal readonly ExposedList<float> clipOutput = new ExposedList<float>(128);
        internal readonly ExposedList<float> clippedVertices = new ExposedList<float>(128);
        internal readonly ExposedList<int> clippedTriangles = new ExposedList<int>(128);
        internal readonly ExposedList<float> clippedUVs = new ExposedList<float>(128);
        internal readonly ExposedList<float> scratch = new ExposedList<float>();

        internal ClippingAttachment clipAttachment;
        internal ExposedList<ExposedList<float>> clippingPolygons;

        public ExposedList<float> ClippedVertices { get { return this.clippedVertices; } }
        public ExposedList<int> ClippedTriangles { get { return this.clippedTriangles; } }
        public ExposedList<float> ClippedUVs { get { return this.clippedUVs; } }

        public bool IsClipping { get { return this.clipAttachment != null; } }

        public int ClipStart(Slot slot, ClippingAttachment clip)
        {
            if (this.clipAttachment != null) return 0;
            this.clipAttachment = clip;

            var n = clip.worldVerticesLength;
            var vertices = this.clippingPolygon.Resize(n).Items;
            clip.ComputeWorldVertices(slot, 0, n, vertices, 0, 2);
            MakeClockwise(this.clippingPolygon);
            this.clippingPolygons = this.triangulator.Decompose(this.clippingPolygon, this.triangulator.Triangulate(this.clippingPolygon));
            foreach (var polygon in this.clippingPolygons)
            {
                MakeClockwise(polygon);
                polygon.Add(polygon.Items[0]);
                polygon.Add(polygon.Items[1]);
            }
            return this.clippingPolygons.Count;
        }

        public void ClipEnd(Slot slot)
        {
            if (this.clipAttachment != null && this.clipAttachment.endSlot == slot.data) this.ClipEnd();
        }

        public void ClipEnd()
        {
            if (this.clipAttachment == null) return;
            this.clipAttachment = null;
            this.clippingPolygons = null;
            this.clippedVertices.Clear();
            this.clippedTriangles.Clear();
            this.clippingPolygon.Clear();
        }

        public void ClipTriangles(float[] vertices, int verticesLength, int[] triangles, int trianglesLength, float[] uvs)
        {
            ExposedList<float> clipOutput = this.clipOutput, clippedVertices = this.clippedVertices;
            var clippedTriangles = this.clippedTriangles;
            var polygons = this.clippingPolygons.Items;
            var polygonsCount = this.clippingPolygons.Count;

            var index = 0;
            clippedVertices.Clear();
            this.clippedUVs.Clear();
            clippedTriangles.Clear();
            //outer:
            for (var i = 0; i < trianglesLength; i += 3)
            {
                var vertexOffset = triangles[i] << 1;
                float x1 = vertices[vertexOffset], y1 = vertices[vertexOffset + 1];
                float u1 = uvs[vertexOffset], v1 = uvs[vertexOffset + 1];

                vertexOffset = triangles[i + 1] << 1;
                float x2 = vertices[vertexOffset], y2 = vertices[vertexOffset + 1];
                float u2 = uvs[vertexOffset], v2 = uvs[vertexOffset + 1];

                vertexOffset = triangles[i + 2] << 1;
                float x3 = vertices[vertexOffset], y3 = vertices[vertexOffset + 1];
                float u3 = uvs[vertexOffset], v3 = uvs[vertexOffset + 1];

                for (var p = 0; p < polygonsCount; p++)
                {
                    var s = clippedVertices.Count;
                    if (this.Clip(x1, y1, x2, y2, x3, y3, polygons[p], clipOutput))
                    {
                        var clipOutputLength = clipOutput.Count;
                        if (clipOutputLength == 0) continue;
                        float d0 = y2 - y3, d1 = x3 - x2, d2 = x1 - x3, d4 = y3 - y1;
                        var d = 1 / (d0 * d2 + d1 * (y1 - y3));

                        var clipOutputCount = clipOutputLength >> 1;
                        var clipOutputItems = clipOutput.Items;
                        var clippedVerticesItems = clippedVertices.Resize(s + clipOutputCount * 2).Items;
                        var clippedUVsItems = this.clippedUVs.Resize(s + clipOutputCount * 2).Items;
                        for (var ii = 0; ii < clipOutputLength; ii += 2)
                        {
                            float x = clipOutputItems[ii], y = clipOutputItems[ii + 1];
                            clippedVerticesItems[s] = x;
                            clippedVerticesItems[s + 1] = y;
                            float c0 = x - x3, c1 = y - y3;
                            var a = (d0 * c0 + d1 * c1) * d;
                            var b = (d4 * c0 + d2 * c1) * d;
                            var c = 1 - a - b;
                            clippedUVsItems[s] = u1 * a + u2 * b + u3 * c;
                            clippedUVsItems[s + 1] = v1 * a + v2 * b + v3 * c;
                            s += 2;
                        }

                        s = clippedTriangles.Count;
                        var clippedTrianglesItems = clippedTriangles.Resize(s + 3 * (clipOutputCount - 2)).Items;
                        clipOutputCount--;
                        for (var ii = 1; ii < clipOutputCount; ii++)
                        {
                            clippedTrianglesItems[s] = index;
                            clippedTrianglesItems[s + 1] = index + ii;
                            clippedTrianglesItems[s + 2] = index + ii + 1;
                            s += 3;
                        }
                        index += clipOutputCount + 1;
                    }
                    else
                    {
                        var clippedVerticesItems = clippedVertices.Resize(s + 3 * 2).Items;
                        var clippedUVsItems = this.clippedUVs.Resize(s + 3 * 2).Items;
                        clippedVerticesItems[s] = x1;
                        clippedVerticesItems[s + 1] = y1;
                        clippedVerticesItems[s + 2] = x2;
                        clippedVerticesItems[s + 3] = y2;
                        clippedVerticesItems[s + 4] = x3;
                        clippedVerticesItems[s + 5] = y3;

                        clippedUVsItems[s] = u1;
                        clippedUVsItems[s + 1] = v1;
                        clippedUVsItems[s + 2] = u2;
                        clippedUVsItems[s + 3] = v2;
                        clippedUVsItems[s + 4] = u3;
                        clippedUVsItems[s + 5] = v3;

                        s = clippedTriangles.Count;
                        var clippedTrianglesItems = clippedTriangles.Resize(s + 3).Items;
                        clippedTrianglesItems[s] = index;
                        clippedTrianglesItems[s + 1] = index + 1;
                        clippedTrianglesItems[s + 2] = index + 2;
                        index += 3;
                        break; //continue outer;
                    }
                }
            }

        }

        /** Clips the input triangle against the convex, clockwise clipping area. If the triangle lies entirely within the clipping
		 * area, false is returned. The clipping area must duplicate the first vertex at the end of the vertices list. */
        internal bool Clip(float x1, float y1, float x2, float y2, float x3, float y3, ExposedList<float> clippingArea, ExposedList<float> output)
        {
            var originalOutput = output;
            var clipped = false;

            // Avoid copy at the end.
            ExposedList<float> input = null;
            if (clippingArea.Count % 4 >= 2)
            {
                input = output;
                output = this.scratch;
            }
            else
            {
                input = this.scratch;
            }

            input.Clear();
            input.Add(x1);
            input.Add(y1);
            input.Add(x2);
            input.Add(y2);
            input.Add(x3);
            input.Add(y3);
            input.Add(x1);
            input.Add(y1);
            output.Clear();

            var clippingVertices = clippingArea.Items;
            var clippingVerticesLast = clippingArea.Count - 4;
            for (var i = 0; ; i += 2)
            {
                float edgeX = clippingVertices[i], edgeY = clippingVertices[i + 1];
                float edgeX2 = clippingVertices[i + 2], edgeY2 = clippingVertices[i + 3];
                float deltaX = edgeX - edgeX2, deltaY = edgeY - edgeY2;

                var inputVertices = input.Items;
                int inputVerticesLength = input.Count - 2, outputStart = output.Count;
                for (var ii = 0; ii < inputVerticesLength; ii += 2)
                {
                    float inputX = inputVertices[ii], inputY = inputVertices[ii + 1];
                    float inputX2 = inputVertices[ii + 2], inputY2 = inputVertices[ii + 3];
                    var side2 = deltaX * (inputY2 - edgeY2) - deltaY * (inputX2 - edgeX2) > 0;
                    if (deltaX * (inputY - edgeY2) - deltaY * (inputX - edgeX2) > 0)
                    {
                        if (side2)
                        { // v1 inside, v2 inside
                            output.Add(inputX2);
                            output.Add(inputY2);
                            continue;
                        }
                        // v1 inside, v2 outside
                        float c0 = inputY2 - inputY, c2 = inputX2 - inputX;
                        var s = c0 * (edgeX2 - edgeX) - c2 * (edgeY2 - edgeY);
                        if (Math.Abs(s) > 0.000001f)
                        {
                            var ua = (c2 * (edgeY - inputY) - c0 * (edgeX - inputX)) / s;
                            output.Add(edgeX + (edgeX2 - edgeX) * ua);
                            output.Add(edgeY + (edgeY2 - edgeY) * ua);
                        }
                        else
                        {
                            output.Add(edgeX);
                            output.Add(edgeY);
                        }
                    }
                    else if (side2)
                    { // v1 outside, v2 inside
                        float c0 = inputY2 - inputY, c2 = inputX2 - inputX;
                        var s = c0 * (edgeX2 - edgeX) - c2 * (edgeY2 - edgeY);
                        if (Math.Abs(s) > 0.000001f)
                        {
                            var ua = (c2 * (edgeY - inputY) - c0 * (edgeX - inputX)) / s;
                            output.Add(edgeX + (edgeX2 - edgeX) * ua);
                            output.Add(edgeY + (edgeY2 - edgeY) * ua);
                        }
                        else
                        {
                            output.Add(edgeX);
                            output.Add(edgeY);
                        }
                        output.Add(inputX2);
                        output.Add(inputY2);
                    }
                    clipped = true;
                }

                if (outputStart == output.Count)
                { // All edges outside.
                    originalOutput.Clear();
                    return true;
                }

                output.Add(output.Items[0]);
                output.Add(output.Items[1]);

                if (i == clippingVerticesLast) break;
                var temp = output;
                output = input;
                output.Clear();
                input = temp;
            }

            if (originalOutput != output)
            {
                originalOutput.Clear();
                for (int i = 0, n = output.Count - 2; i < n; i++)
                {
                    originalOutput.Add(output.Items[i]);
                }
            }
            else
            {
                originalOutput.Resize(originalOutput.Count - 2);
            }

            return clipped;
        }

        public static void MakeClockwise(ExposedList<float> polygon)
        {
            var vertices = polygon.Items;
            var verticeslength = polygon.Count;

            float area = vertices[verticeslength - 2] * vertices[1] - vertices[0] * vertices[verticeslength - 1], p1x, p1y, p2x, p2y;
            for (int i = 0, n = verticeslength - 3; i < n; i += 2)
            {
                p1x = vertices[i];
                p1y = vertices[i + 1];
                p2x = vertices[i + 2];
                p2y = vertices[i + 3];
                area += p1x * p2y - p2x * p1y;
            }
            if (area < 0) return;

            for (int i = 0, lastX = verticeslength - 2, n = verticeslength >> 1; i < n; i += 2)
            {
                float x = vertices[i], y = vertices[i + 1];
                var other = lastX - i;
                vertices[i] = vertices[other];
                vertices[i + 1] = vertices[other + 1];
                vertices[other] = x;
                vertices[other + 1] = y;
            }
        }
    }
}
