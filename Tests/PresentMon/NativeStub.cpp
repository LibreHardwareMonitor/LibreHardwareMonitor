// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.

// A deterministic C ABI test double. It does not load PresentMon, start ETW,
// modify the registry, or require a running game/service.
#include <windows.h>
#include <cstdint>
#include <cstring>
#include <limits>

#define API extern "C" __declspec(dllexport)
struct Element { int metric, stat; uint32_t device, index; uint64_t offset, size; };
struct Version { uint16_t major, minor, patch; char tag[22], hash[8], config[4]; };
static_assert(sizeof(Element) == 32);
static_assert(sizeof(Version) == 40);
static int mode, sessions, queries, frameQueries, tracked;
API void Configure(int value) { mode = value; }
API int Resources() { return sessions + queries + frameQueries + tracked; }
API int pmGetApiVersion(Version* v) { *v = {}; v->major = mode == 4 ? 4 : 3; v->minor = 4; return 0; }
API int pmOpenSession(void** h) { *h = (void*)1; ++sessions; return 0; }
API int pmCloseSession(void*) { --sessions; return 0; }
API int pmStartTrackingProcess(void*, uint32_t) { ++tracked; return 0; }
API int pmStopTrackingProcess(void*, uint32_t) { --tracked; return 0; }
API int pmSetEtwFlushPeriod(void*, uint32_t period) { return period == 100 ? 0 : 2; }
API int pmRegisterDynamicQuery(void*, void** h, Element* e, uint64_t n, double window, double offset)
{
    if (mode == 5) return 21;
    if (n != 3 || window != 1000 || offset != 200 || e[0].metric != 1 ||
        e[1].metric != 12 || e[2].metric != 11 || e[0].stat != 12 || e[1].stat != 1) return 2;
    *h = (void*)2; ++queries;
    for (uint64_t i = 0; i < n; ++i) { e[i].offset = 16 + i * 16; e[i].size = mode == 6 ? 4 : 8; }
    return 0;
}
API int pmFreeDynamicQuery(void*) { --queries; return 0; }
API int pmRegisterFrameQuery(void*, void** h, Element* e, uint64_t n, uint32_t* size)
{
    if (n != 1 || e[0].metric != 7 || e[0].stat != 0) return 2;
    *h = (void*)3; ++frameQueries; e[0].offset = 16; e[0].size = 8; *size = 32; return 0;
}
API int pmFreeFrameQuery(void*) { --frameQueries; return 0; }
API int pmConsumeFrames(void*, uint32_t, uint8_t* blob, uint32_t* count)
{
    LARGE_INTEGER qpc, frequency;
    QueryPerformanceCounter(&qpc); QueryPerformanceFrequency(&frequency);
    if (mode == 1) qpc.QuadPart -= 3 * frequency.QuadPart;
    std::memcpy(blob + 16, &qpc.QuadPart, 8);
    *count = mode == 9 ? 257 : 1;
    return 0;
}
API int pmPollDynamicQuery(void*, uint32_t, uint8_t* blob, uint32_t* count)
{
    if (mode == 3) return 13;
    if (*count < 3) return 11;
    *count = mode == 7 ? 0 : 3;
    for (int i = 0; i < 3; ++i)
    {
        uint64_t address = 0x1000 + i;
        double presented = i == 0 ? 60.0 : i == 1 ? 144.0 : std::numeric_limits<double>::quiet_NaN();
        double displayed = presented * 2;
        if (mode == 2) presented = -1;
        if (mode == 8) displayed = std::numeric_limits<double>::infinity();
        std::memcpy(blob + i * 64 + 16, &address, 8);
        std::memcpy(blob + i * 64 + 32, &presented, 8);
        std::memcpy(blob + i * 64 + 48, &displayed, 8);
    }
    return 0;
}
