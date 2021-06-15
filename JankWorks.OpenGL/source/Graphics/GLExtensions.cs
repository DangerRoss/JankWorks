﻿using System;
using JankWorks.Graphics;

using static OpenGL.Constants;

namespace JankWorks.Drivers.OpenGL.Graphics
{
    static class GLExtensions
    {
        public static int GetGLType<T>()
        {
            var type = typeof(T);

            if (type == typeof(byte))
            {
                return GL_UNSIGNED_BYTE;
            }
            else if (type == typeof(ushort))
            {
                return GL_UNSIGNED_SHORT;
            }
            else if (type == typeof(uint))
            {
                return GL_UNSIGNED_INT;
            }
            else if (type == typeof(sbyte))
            {
                return GL_BYTE;
            }
            else if (type == typeof(short))
            {
                return GL_SHORT;
            }
            else if (type == typeof(int))
            {
                return GL_INT;
            }
            else if (type == typeof(float))
            {
                return GL_FLOAT;
            }
            else if(type == typeof(double))
            {
                return GL_DOUBLE;
            }

            throw new InvalidOperationException();
        }

        public static int GetGLPrimitive(this DrawPrimitiveType primitive)
        {
            return primitive switch
            {
                DrawPrimitiveType.Points => GL_POINTS,
                DrawPrimitiveType.Lines => GL_LINES,
                DrawPrimitiveType.LineLoop => GL_LINE_LOOP,
                DrawPrimitiveType.LineStrip => GL_LINE_STRIP,
                DrawPrimitiveType.Triangles => GL_TRIANGLES,
                DrawPrimitiveType.TriangleStrip => GL_TRIANGLE_STRIP,

                _ => throw new NotImplementedException()
            };
        }

        public static int GetGLPointerType(this VertexAttributeFormat type)
        {
            return type switch
            {
                VertexAttributeFormat.Byte => GL_BYTE,
                VertexAttributeFormat.Short => GL_SHORT,
                VertexAttributeFormat.Int => GL_INT,
                VertexAttributeFormat.Float => GL_FLOAT,
                VertexAttributeFormat.Double => GL_DOUBLE,

                VertexAttributeFormat.UByte => GL_UNSIGNED_BYTE,
                VertexAttributeFormat.UShort => GL_UNSIGNED_SHORT,
                VertexAttributeFormat.UInt => GL_UNSIGNED_INT,

                VertexAttributeFormat.Vector2f => GL_FLOAT,
                VertexAttributeFormat.Vector2i => GL_INT,

                VertexAttributeFormat.Vector3f => GL_FLOAT,
                VertexAttributeFormat.Vector3i => GL_INT,

                VertexAttributeFormat.Vector4f => GL_FLOAT,
                VertexAttributeFormat.Vector4i => GL_INT,

                _ => throw new NotImplementedException()
            };
        }

        public static int GetGLBufferUsage(this BufferUsage usage)
        {
            return usage switch
            {
                BufferUsage.Static => GL_STATIC_DRAW,
                BufferUsage.Dynamic => GL_DYNAMIC_DRAW,
                BufferUsage.Stream => GL_STREAM_DRAW,

                _ => throw new NotImplementedException()
            };
        }

        public static uint GetGLClearBits(this ClearBitMask mode)
        {
            uint value = 0;

            if (mode.HasFlag(ClearBitMask.Colour)) { value |= GL_COLOR_BUFFER_BIT; }

            if (mode.HasFlag(ClearBitMask.Depth)) { value |= GL_DEPTH_BUFFER_BIT; }

            if (mode.HasFlag(ClearBitMask.Stencil)) { value |= GL_STENCIL_BUFFER_BIT; }

            return value;
        }
    }
}
