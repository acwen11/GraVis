
static const float PI = 3.14159265f;

struct Ray
{
    float3 origin;
    float3 direction;
    float3 invdir;
    int3 sign;
};

bool IsPointInBox(float3 p, float3 extents) // Box-center in origin (0,0,0)
{
	return p.x < extents.x&& p.x > -extents.x && p.y < extents.y&& p.y > -extents.y && p.z < extents.z&& p.z > -extents.z;
}

bool IsPointInBox(float3 p, float3 extents, float3 boxCenter) // Box-center in arbitrary center
{
	p -= boxCenter;
	return p.x < extents.x&& p.x > -extents.x && p.y < extents.y&& p.y > -extents.y && p.z < extents.z&& p.z > -extents.z;
}

// compute the near and far intersections of the cube (stored in the x and y components) using the slab method
// no intersection means vec.x > vec.y (really tNear > tFar)
float2 intersectAABB(Ray ray, float3 boxExtents)
{
    float3 tMin = (-boxExtents - ray.origin) / ray.direction;
    float3 tMax = (boxExtents - ray.origin) / ray.direction;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return float2(tNear, tFar);
};