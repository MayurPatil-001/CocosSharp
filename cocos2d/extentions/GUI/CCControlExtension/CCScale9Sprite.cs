using System.Diagnostics;

namespace Cocos2D
{
    /// <summary>
    /// This is a special sprite container that represents a 9 point sprite region, where 8 of hte
    /// points are along the perimeter, and the 9th is the center area. This special sprite is capable of resizing
    /// itself to arbitrary scales.
    /// </summary>
    public class CCScale9Sprite : CCNodeRGBA
    {
        protected CCSprite _bottom;
        protected CCSprite _bottomLeft;
        protected CCSprite _bottomRight;
        protected CCSprite _centre;
        protected CCSprite _left;

        protected bool _opacityModifyRGB;
        protected bool m_bSpriteFrameRotated;
        protected bool m_bSpritesGenerated;
        protected byte _opacity = 255;

        /** 
        * The end-cap insets. 
        * On a non-resizeable sprite, this property is set to CGRectZero; the sprite 
        * does not use end caps and the entire sprite is subject to stretching. 
        */
        private CCRect m_capInsets;
        protected CCRect m_capInsetsInternal;
        private float m_insetBottom;
        private float m_insetLeft;
        private float m_insetRight;
        private float m_insetTop;
        private CCSize m_originalSize;
        protected bool m_positionsAreDirty;
        private CCSize m_preferredSize;
        protected CCColor3B m_sColorUnmodified;
        protected CCRect m_spriteRect;
        protected CCColor3B m_tColor = CCTypes.CCWhite;
        protected CCSprite _right;
        protected CCSpriteBatchNode _scale9Image;
        protected CCSprite _top;
        protected CCSprite _topLeft;
        protected CCSprite _topRight;

        public override CCSize ContentSize
        {
            get { return base.ContentSize; }
            set
            {
                base.ContentSize = value;
                m_positionsAreDirty = true;
            }
        }

        public CCSize PreferredSize
        {
            get { return m_preferredSize; }
            set
            {
                ContentSize = value;
                m_preferredSize = value;
            }
        }

        public CCRect CapInsets
        {
            get { return m_capInsets; }
            set
            {
                CCSize contentSize = m_obContentSize;
                UpdateWithBatchNode(_scale9Image, m_spriteRect, m_bSpriteFrameRotated, value);
                ContentSize = contentSize;
            }
        }

        public float InsetLeft
        {
            set
            {
                m_insetLeft = value;
                UpdateCapInset();
            }
            get { return m_insetLeft; }
        }

        public float InsetTop
        {
            set
            {
                m_insetTop = value;
                UpdateCapInset();
            }
            get { return m_insetTop; }
        }

        public float InsetRight
        {
            set
            {
                m_insetRight = value;
                UpdateCapInset();
            }
            get { return m_insetRight; }
        }

        public float InsetBottom
        {
            set
            {
                m_insetBottom = value;
                UpdateCapInset();
            }
            get { return m_insetBottom; }
        }

        #region RGBA protocol

        public override CCColor3B Color
        {
            get { return m_tColor; }
            set
            {
                m_tColor = value;
                if (_scale9Image != null && _scale9Image.Children != null && _scale9Image.Children.count != 0)
                {
                    for (int i = 0; i < _scale9Image.Children.count; i++)
                    {
                        var prot = _scale9Image.Children[i] as ICCRGBAProtocol;
                        if (prot != null)
                        {
                            prot.Color = value;
                        }
                    }
                }
            }
        }

        public override byte Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
                if (_scale9Image != null && _scale9Image.Children != null && _scale9Image.Children.count != 0)
                {
                    for (int i = 0; i < _scale9Image.Children.count; i++)
                    {
                        var prot = _scale9Image.Children[i] as ICCRGBAProtocol;
                        if (prot != null)
                        {
                            prot.Opacity = value;
                        }
                    }
                }
            }
        }

        public override bool IsOpacityModifyRGB
        {
            get { return _opacityModifyRGB; }
            set
            {
                _opacityModifyRGB = value;
                if (_scale9Image != null && _scale9Image.Children != null && _scale9Image.Children.count != 0)
                {
                    for (int i = 0; i < _scale9Image.Children.count; i++)
                    {
                        var prot = _scale9Image.Children[i] as ICCRGBAProtocol;
                        if (prot != null)
                        {
                            prot.IsOpacityModifyRGB = value;
                        }
                    }
                }
            }
        }

        #endregion

        public override bool Init()
        {
            return InitWithBatchNode(null, CCRect.Zero, CCRect.Zero);
        }

        public bool InitWithBatchNode(CCSpriteBatchNode batchnode, CCRect rect, CCRect capInsets)
        {
            return InitWithBatchNode(batchnode, rect, false, capInsets);
        }

        public bool InitWithBatchNode(CCSpriteBatchNode batchnode, CCRect rect, bool rotated, CCRect capInsets)
        {
            base.Init();

            if (batchnode != null)
            {
                UpdateWithBatchNode(batchnode, rect, rotated, capInsets);
                AnchorPoint = new CCPoint(0.5f, 0.5f);
            }
            m_positionsAreDirty = true;

            return true;
        }

        public bool UpdateWithBatchNode(CCSpriteBatchNode batchnode, CCRect rect, bool rotated, CCRect capInsets)
        {
            var opacity = Opacity;
            var color = Color;

            // Release old sprites
            RemoveAllChildrenWithCleanup(true);

            _scale9Image = batchnode;

            _scale9Image.RemoveAllChildrenWithCleanup(true);

            m_capInsets = capInsets;

            // If there is no given rect
            if (rect.Equals(CCRect.Zero))
            {
                // Get the texture size as original
                CCSize textureSize = _scale9Image.TextureAtlas.Texture.ContentSize;

                rect = new CCRect(0, 0, textureSize.Width, textureSize.Height);
            }

            // Set the given rect's size as original size
            m_spriteRect = rect;
            m_originalSize = rect.Size;
            m_preferredSize = m_originalSize;
            m_capInsetsInternal = capInsets;

            float h = rect.Size.Height;
            float w = rect.Size.Width;

            // If there is no specified center region
            if (m_capInsetsInternal.Equals(CCRect.Zero))
            {
                m_capInsetsInternal = new CCRect(w / 3, h / 3, w / 3, h / 3);
            }

            float left_w = m_capInsetsInternal.Origin.X;
            float center_w = m_capInsetsInternal.Size.Width;
            float right_w = rect.Size.Width - (left_w + center_w);

            float top_h = m_capInsetsInternal.Origin.Y;
            float center_h = m_capInsetsInternal.Size.Height;
            float bottom_h = rect.Size.Height - (top_h + center_h);

            // calculate rects

            // ... top row
            float x = 0.0f;
            float y = 0.0f;

            // top left
            CCRect lefttopbounds = new CCRect(x, y,
                                              left_w, top_h);

            // top center
            x += left_w;
            CCRect centertopbounds = new CCRect(x, y,
                                                center_w, top_h);

            // top right
            x += center_w;
            CCRect righttopbounds = new CCRect(x, y,
                                               right_w, top_h);

            // ... center row
            x = 0.0f;
            y = 0.0f;
            y += top_h;

            // center left
            CCRect leftcenterbounds = new CCRect(x, y,
                                                 left_w, center_h);

            // center center
            x += left_w;
            CCRect centerbounds = new CCRect(x, y,
                                             center_w, center_h);

            // center right
            x += center_w;
            CCRect rightcenterbounds = new CCRect(x, y,
                                                  right_w, center_h);

            // ... bottom row
            x = 0.0f;
            y = 0.0f;
            y += top_h;
            y += center_h;

            // bottom left
            CCRect leftbottombounds = new CCRect(x, y,
                                                 left_w, bottom_h);

            // bottom center
            x += left_w;
            CCRect centerbottombounds = new CCRect(x, y,
                                                   center_w, bottom_h);

            // bottom right
            x += center_w;
            CCRect rightbottombounds = new CCRect(x, y,
                                                  right_w, bottom_h);

            if (!rotated)
            {
                // CCLog("!rotated");

                CCAffineTransform t = CCAffineTransform.Identity;
                t = CCAffineTransform.Translate(t, rect.Origin.X, rect.Origin.Y);

                centerbounds = CCAffineTransform.Transform(centerbounds, t);
                rightbottombounds = CCAffineTransform.Transform(rightbottombounds, t);
                leftbottombounds = CCAffineTransform.Transform(leftbottombounds, t);
                righttopbounds = CCAffineTransform.Transform(righttopbounds, t);
                lefttopbounds = CCAffineTransform.Transform(lefttopbounds, t);
                rightcenterbounds = CCAffineTransform.Transform(rightcenterbounds, t);
                leftcenterbounds = CCAffineTransform.Transform(leftcenterbounds, t);
                centerbottombounds = CCAffineTransform.Transform(centerbottombounds, t);
                centertopbounds = CCAffineTransform.Transform(centertopbounds, t);

                // Centre
                _centre = new CCSprite();
                _centre.InitWithTexture(_scale9Image.Texture, centerbounds);
                _scale9Image.AddChild(_centre, 0, (int)Positions.Centre);

                // Top
                _top = new CCSprite();
                _top.InitWithTexture(_scale9Image.Texture, centertopbounds);
                _scale9Image.AddChild(_top, 1, (int)Positions.Top);

                // Bottom
                _bottom = new CCSprite();
                _bottom.InitWithTexture(_scale9Image.Texture, centerbottombounds);
                _scale9Image.AddChild(_bottom, 1, (int)Positions.Bottom);

                // Left
                _left = new CCSprite();
                _left.InitWithTexture(_scale9Image.Texture, leftcenterbounds);
                _scale9Image.AddChild(_left, 1, (int)Positions.Left);

                // Right
                _right = new CCSprite();
                _right.InitWithTexture(_scale9Image.Texture, rightcenterbounds);
                _scale9Image.AddChild(_right, 1, (int)Positions.Right);

                // Top left
                _topLeft = new CCSprite();
                _topLeft.InitWithTexture(_scale9Image.Texture, lefttopbounds);
                _scale9Image.AddChild(_topLeft, 2, (int)Positions.TopLeft);

                // Top right
                _topRight = new CCSprite();
                _topRight.InitWithTexture(_scale9Image.Texture, righttopbounds);
                _scale9Image.AddChild(_topRight, 2, (int)Positions.TopRight);

                // Bottom left
                _bottomLeft = new CCSprite();
                _bottomLeft.InitWithTexture(_scale9Image.Texture, leftbottombounds);
                _scale9Image.AddChild(_bottomLeft, 2, (int)Positions.BottomLeft);

                // Bottom right
                _bottomRight = new CCSprite();
                _bottomRight.InitWithTexture(_scale9Image.Texture, rightbottombounds);
                _scale9Image.AddChild(_bottomRight, 2, (int)Positions.BottomRight);
            }
            else
            {
                // set up transformation of coordinates
                // to handle the case where the sprite is stored rotated
                // in the spritesheet
                // CCLog("rotated");

                CCAffineTransform t = CCAffineTransform.Identity;

                CCRect rotatedcenterbounds = centerbounds;
                CCRect rotatedrightbottombounds = rightbottombounds;
                CCRect rotatedleftbottombounds = leftbottombounds;
                CCRect rotatedrighttopbounds = righttopbounds;
                CCRect rotatedlefttopbounds = lefttopbounds;
                CCRect rotatedrightcenterbounds = rightcenterbounds;
                CCRect rotatedleftcenterbounds = leftcenterbounds;
                CCRect rotatedcenterbottombounds = centerbottombounds;
                CCRect rotatedcentertopbounds = centertopbounds;

                t = CCAffineTransform.Translate(t, rect.Size.Height + rect.Origin.X, rect.Origin.Y);
                t = CCAffineTransform.Rotate(t, 1.57079633f);

                centerbounds = CCAffineTransform.Transform(centerbounds, t);
                rightbottombounds = CCAffineTransform.Transform(rightbottombounds, t);
                leftbottombounds = CCAffineTransform.Transform(leftbottombounds, t);
                righttopbounds = CCAffineTransform.Transform(righttopbounds, t);
                lefttopbounds = CCAffineTransform.Transform(lefttopbounds, t);
                rightcenterbounds = CCAffineTransform.Transform(rightcenterbounds, t);
                leftcenterbounds = CCAffineTransform.Transform(leftcenterbounds, t);
                centerbottombounds = CCAffineTransform.Transform(centerbottombounds, t);
                centertopbounds = CCAffineTransform.Transform(centertopbounds, t);

                rotatedcenterbounds.Origin = centerbounds.Origin;
                rotatedrightbottombounds.Origin = rightbottombounds.Origin;
                rotatedleftbottombounds.Origin = leftbottombounds.Origin;
                rotatedrighttopbounds.Origin = righttopbounds.Origin;
                rotatedlefttopbounds.Origin = lefttopbounds.Origin;
                rotatedrightcenterbounds.Origin = rightcenterbounds.Origin;
                rotatedleftcenterbounds.Origin = leftcenterbounds.Origin;
                rotatedcenterbottombounds.Origin = centerbottombounds.Origin;
                rotatedcentertopbounds.Origin = centertopbounds.Origin;

                // Centre
                _centre = new CCSprite();
                _centre.InitWithTexture(_scale9Image.Texture, rotatedcenterbounds, true);
                _scale9Image.AddChild(_centre, 0, (int)Positions.Centre);

                // Top
                _top = new CCSprite();
                _top.InitWithTexture(_scale9Image.Texture, rotatedcentertopbounds, true);
                _scale9Image.AddChild(_top, 1, (int)Positions.Top);

                // Bottom
                _bottom = new CCSprite();
                _bottom.InitWithTexture(_scale9Image.Texture, rotatedcenterbottombounds, true);
                _scale9Image.AddChild(_bottom, 1, (int)Positions.Bottom);

                // Left
                _left = new CCSprite();
                _left.InitWithTexture(_scale9Image.Texture, rotatedleftcenterbounds, true);
                _scale9Image.AddChild(_left, 1, (int)Positions.Left);

                // Right
                _right = new CCSprite();
                _right.InitWithTexture(_scale9Image.Texture, rotatedrightcenterbounds, true);
                _scale9Image.AddChild(_right, 1, (int)Positions.Right);

                // Top left
                _topLeft = new CCSprite();
                _topLeft.InitWithTexture(_scale9Image.Texture, rotatedlefttopbounds, true);
                _scale9Image.AddChild(_topLeft, 2, (int)Positions.TopLeft);

                // Top right
                _topRight = new CCSprite();
                _topRight.InitWithTexture(_scale9Image.Texture, rotatedrighttopbounds, true);
                _scale9Image.AddChild(_topRight, 2, (int)Positions.TopRight);

                // Bottom left
                _bottomLeft = new CCSprite();
                _bottomLeft.InitWithTexture(_scale9Image.Texture, rotatedleftbottombounds, true);
                _scale9Image.AddChild(_bottomLeft, 2, (int)Positions.BottomLeft);

                // Bottom right
                _bottomRight = new CCSprite();
                _bottomRight.InitWithTexture(_scale9Image.Texture, rotatedrightbottombounds, true);
                _scale9Image.AddChild(_bottomRight, 2, (int)Positions.BottomRight);
            }
            
            ContentSize = rect.Size;
            AddChild(_scale9Image);

            if (m_bSpritesGenerated)
            {
                // Restore color and opacity
                Opacity = opacity;
                Color = color;
            }
            m_bSpritesGenerated = true;

            return true;
        }

        protected void UpdatePositions()
        {
            // Check that instances are non-NULL
            if (!((_topLeft != null) &&
                 (_topRight != null) &&
                 (_bottomRight != null) &&
                 (_bottomLeft != null) &&
                 (_centre != null)))
            {
                // if any of the above sprites are NULL, return
                return;
            }
            
            CCSize size = m_obContentSize;

            float sizableWidth = size.Width - _topLeft.ContentSize.Width - _topRight.ContentSize.Width;
            float sizableHeight = size.Height - _topLeft.ContentSize.Height - _bottomRight.ContentSize.Height;

            float horizontalScale = sizableWidth / _centre.ContentSize.Width;
            float verticalScale = sizableHeight / _centre.ContentSize.Height;

            _centre.ScaleX = horizontalScale;
            _centre.ScaleY = verticalScale;

            float rescaledWidth = _centre.ContentSize.Width * horizontalScale;
            float rescaledHeight = _centre.ContentSize.Height * verticalScale;

            float leftWidth = _bottomLeft.ContentSize.Width;
            float bottomHeight = _bottomLeft.ContentSize.Height;

            _bottomLeft.AnchorPoint = CCPoint.Zero;
            _bottomRight.AnchorPoint = CCPoint.Zero;
            _topLeft.AnchorPoint = CCPoint.Zero;
            _topRight.AnchorPoint = CCPoint.Zero;
            _left.AnchorPoint = CCPoint.Zero;
            _right.AnchorPoint = CCPoint.Zero;
            _top.AnchorPoint = CCPoint.Zero;
            _bottom.AnchorPoint = CCPoint.Zero;
            _centre.AnchorPoint = CCPoint.Zero;

            // Position corners
            _bottomLeft.Position = CCPoint.Zero;
            _bottomRight.Position = new CCPoint(leftWidth + rescaledWidth, 0);
            _topLeft.Position = new CCPoint(0, bottomHeight + rescaledHeight);
            _topRight.Position = new CCPoint(leftWidth + rescaledWidth, bottomHeight + rescaledHeight);

            // Scale and position borders
            _left.Position = new CCPoint(0, bottomHeight);
            _left.ScaleY = verticalScale;
            _right.Position = new CCPoint(leftWidth + rescaledWidth, bottomHeight);
            _right.ScaleY = verticalScale;
            _bottom.Position = new CCPoint(leftWidth, 0);
            _bottom.ScaleX = horizontalScale;
            _top.Position = new CCPoint(leftWidth, bottomHeight + rescaledHeight);
            _top.ScaleX = horizontalScale;

            // Position centre
            _centre.Position = new CCPoint(leftWidth, bottomHeight);
        }

        #region File Init Methods
        internal virtual bool InitWithFile(string file, CCRect rect, CCRect capInsets)
        {
            Debug.Assert(!string.IsNullOrEmpty(file), "Invalid file for sprite");

            CCSpriteBatchNode batchnode = new CCSpriteBatchNode(file, 9);
            bool pReturn = InitWithBatchNode(batchnode, rect, capInsets);
            return pReturn;
        }

        internal virtual bool InitWithFile(string file, CCRect rect)
        {
            Debug.Assert(!string.IsNullOrEmpty(file), "Invalid file for sprite");
            bool pReturn = InitWithFile(file, rect, CCRect.Zero);
            return pReturn;
        }

        internal virtual bool InitWithFile(CCRect capInsets, string file)
        {
            bool pReturn = InitWithFile(file, CCRect.Zero, capInsets);
            return pReturn;
        }

        internal virtual bool InitWithFile(string file)
        {
            bool pReturn = InitWithFile(file, CCRect.Zero);
            return pReturn;
        }
        #endregion

        #region SpriteFrame Methods

        internal virtual bool InitWithSpriteFrame(CCSpriteFrame spriteFrame, CCRect capInsets)
        {
            Debug.Assert(spriteFrame != null, "Sprite frame must be not nil");

            CCSpriteBatchNode batchnode = new CCSpriteBatchNode(spriteFrame.Texture, 9);
            bool pReturn = InitWithBatchNode(batchnode, spriteFrame.Rect, spriteFrame.IsRotated, capInsets);
            return pReturn;
        }

        internal virtual bool InitWithSpriteFrame(CCSpriteFrame spriteFrame)
        {
            Debug.Assert(spriteFrame != null, "Invalid spriteFrame for sprite");
            bool pReturn = InitWithSpriteFrame(spriteFrame, CCRect.Zero);
            return pReturn;
        }

        internal virtual bool InitWithSpriteFrameName(string spriteFrameName, CCRect capInsets)
        {
            Debug.Assert(spriteFrameName != null, "Invalid spriteFrameName for sprite");

            CCSpriteFrame frame = CCSpriteFrameCache.SharedSpriteFrameCache.SpriteFrameByName(spriteFrameName);
            bool pReturn = InitWithSpriteFrame(frame, capInsets);
            return pReturn;
        }

        internal virtual bool InitWithSpriteFrameName(string spriteFrameName)
        {
            bool pReturn = InitWithSpriteFrameName(spriteFrameName, CCRect.Zero);
            return pReturn;
        }

        #endregion

        public CCScale9Sprite(CCRect capInsets)
        {
            InitWithBatchNode(_scale9Image, m_spriteRect, capInsets);
        }

        public CCScale9Sprite()
        {
            Init();
        }

        protected void UpdateCapInset()
        {
            CCRect insets;
            if (m_insetLeft == 0 && m_insetTop == 0 && m_insetRight == 0 && m_insetBottom == 0)
            {
                insets = CCRect.Zero;
            }
            else
            {
                if (m_bSpriteFrameRotated)
                {
                    insets = new CCRect(m_insetBottom,
                                        m_insetLeft,
                                        m_spriteRect.Size.Width - m_insetRight - m_insetLeft,
                                        m_spriteRect.Size.Height - m_insetTop - m_insetBottom);
                }
                else
                {
                    insets = new CCRect(m_insetLeft,
                                        m_insetTop,
                                        m_spriteRect.Size.Width - m_insetLeft - m_insetRight,
                                        m_spriteRect.Size.Height - m_insetTop - m_insetBottom);
                }
            }
            CapInsets = insets;
        }


        public void SetSpriteFrame(CCSpriteFrame spriteFrame)
        {
            CCSpriteBatchNode batchnode = new CCSpriteBatchNode(spriteFrame.Texture, 9);
            UpdateWithBatchNode(batchnode, spriteFrame.Rect, spriteFrame.IsRotated, CCRect.Zero);

            // Reset insets
            m_insetLeft = 0;
            m_insetTop = 0;
            m_insetRight = 0;
            m_insetBottom = 0;
        }

        public override void Visit()
        {
            if (m_positionsAreDirty)
            {
                UpdatePositions();
                m_positionsAreDirty = false;
            }
            base.Visit();
        }

        #region Nested type: Positions

        private enum Positions
        {
            Centre = 0,
            Top,
            Left,
            Right,
            Bottom,
            TopRight,
            TopLeft,
            BottomRight,
            BottomLeft
        };

        #endregion
    }
}