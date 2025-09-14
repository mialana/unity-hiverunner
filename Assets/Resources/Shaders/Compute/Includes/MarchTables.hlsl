// Values from http://paulbourke.net/geometry/polygonise/

static const int cornerIndexAFromEdge[12] = {
    0,
    1,
    2,
    3,
    4,
    5,
    6,
    7,
    0,
    1,
    2,
    3
};

static const int cornerIndexBFromEdge[12] = {
    1,
    2,
    3,
    0,
    5,
    6,
    7,
    4,
    4,
    5,
    6,
    7
};