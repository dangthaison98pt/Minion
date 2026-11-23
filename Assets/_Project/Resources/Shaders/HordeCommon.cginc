#ifndef HORDE_COMMON_INCLUDED
#define HORDE_COMMON_INCLUDED

// 6 đỉnh = 2 tam giác tạo thành 1 hình vuông. Không cần Mesh: đỉnh được tạo từ SV_VertexID.
static const float2 kCorners[6] =
{
    float2(-0.5, -0.5), float2(-0.5,  0.5), float2( 0.5,  0.5),
    float2(-0.5, -0.5), float2( 0.5,  0.5), float2( 0.5, -0.5)
};

// Bám vào lưới pixel nghệ thuật để hình luôn sắc nét
float2 SnapToPixel(float2 p, float ppu) { return round(p * ppu) / ppu; }

// Lính đứng thấp hơn trên màn hình (y nhỏ) phải che lính đứng cao hơn → dùng y làm độ sâu
float DepthFromY(float y) { return y * 0.01; }

#endif
