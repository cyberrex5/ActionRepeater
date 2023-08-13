#include "pch.h"
#include "PathWindow.h"
#include <string>

//#define MEASURE_RENDER
#ifdef MEASURE_RENDER
#include <chrono>
#include <string>
#endif

#define HR(rval) {\
                     hr = rval;\
                     if (FAILED(hr)) return hr;\
                 }

using namespace PathWindows;
using Microsoft::WRL::ComPtr;

PathWindow::PathWindow(const std::function<void(HWND, UINT, WPARAM, LPARAM)>& onUnhandledMsg, bool clickable) :
    WND_WIDTH(GetSystemMetrics(SM_CXVIRTUALSCREEN)),
    WND_HEIGHT(GetSystemMetrics(SM_CYVIRTUALSCREEN)),

    CLICKABLE(clickable),

    m_info(WND_WIDTH, WND_HEIGHT),

    m_hWnd(nullptr),

    m_pD2Factory(nullptr),
    m_pWICFactory(nullptr),
    m_pStrokeStyle(nullptr),

    m_pRenderTarget(nullptr),
    m_pInteropTarget(nullptr),
    m_pPathBrush(nullptr),

    m_onUnhandledMsg(onUnhandledMsg)
{}

PathWindow::~PathWindow()
{
    SafeRelease(&m_pD2Factory);
    SafeRelease(&m_pWICFactory);
    SafeRelease(&m_pStrokeStyle);

    DiscardDeviceResources();
}

HWND PathWindow::GetHandle()
{
    return m_hWnd;
}

HRESULT PathWindow::Initialize()
{
    HRESULT hr;

    HR(CreateDeviceIndependentResources());

    WNDCLASSEX wcex{};
    wcex.cbSize = sizeof(WNDCLASSEX);
    wcex.style = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc = PathWindow::WndProc;
    wcex.hInstance = HINST_THISCOMPONENT;
    wcex.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wcex.lpszClassName = L"PathWindow";

    RegisterClassEx(&wcex);


    // In terms of using the correct DPI, to create a window at a specific size like this, the procedure is to first create the window hidden. Then we get
    // the actual DPI from the HWND (which will be assigned by whichever monitor the window is created on). Then we use SetWindowPos to resize it to the
    // correct DPI-scaled size, then we use ShowWindow to show it.

    m_hWnd = CreateWindowEx(
        (CLICKABLE ? WS_EX_TOOLWINDOW : WS_EX_TRANSPARENT | WS_EX_NOACTIVATE) | WS_EX_NOREDIRECTIONBITMAP | WS_EX_LAYERED | WS_EX_TOPMOST,
        wcex.lpszClassName,
        L"ActionRepeater Path Window",
        WS_POPUP,
        0,
        0,
        0,
        0,
        nullptr,
        nullptr,
        HINST_THISCOMPONENT,
        this
    );

    if (!m_hWnd) return hr;

    float dpi = static_cast<float>(GetDpiForWindow(m_hWnd));
    int primaryMonitorX = GetSystemMetrics(SM_CXSCREEN);
    int primaryMonitorY = GetSystemMetrics(SM_CYSCREEN);

    SetWindowPos(m_hWnd, HWND_TOPMOST, primaryMonitorX - WND_WIDTH, primaryMonitorY - WND_HEIGHT, static_cast<int>(WND_WIDTH * dpi / 96.0f), static_cast<int>(WND_HEIGHT * dpi / 96.0f), 0);
    ShowWindow(m_hWnd, SW_SHOWNORMAL);
    hr = Render();

    return hr;
}

HRESULT PathWindow::RunMessageLoop()
{
    MSG msg{};
    BOOL bRet;
    while ((bRet = GetMessage(&msg, NULL, 0, 0)) != 0)
    {
        if (bRet == -1)
        {
            return E_FAIL;
        }

        //TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return S_OK;
}

inline std::vector<D2D1_POINT_2F>& PathWindow::GetLastPath()
{
    if (m_paths.size() == 0)
    {
        m_paths.push_back(std::vector<D2D1_POINT_2F> {});
        return m_paths[0];
    }

    return m_paths[m_paths.size() - 1];
}

HRESULT PathWindow::AddPoint(POINT point, bool render, bool newPath)
{
    D2D1_POINT_2F d2Point = D2D1::Point2F(static_cast<float>(point.x), static_cast<float>(point.y));

    if (newPath)
    {
        m_paths.push_back(std::vector<D2D1_POINT_2F> { d2Point });
    }
    else
    {
        GetLastPath().push_back(d2Point);
    }

    if (render) return Render();
    return S_OK;
}

HRESULT PathWindow::AddPoints(POINT* points, int length)
{
    if (length < 1) return E_INVALIDARG;
    if (!points) return E_INVALIDARG;

    for (int i = 0; i < length; ++i)
    {
        POINT point = points[i];
        GetLastPath().push_back(D2D1::Point2F(static_cast<float>(point.x), static_cast<float>(point.y)));
    }

    return Render();
}

HRESULT PathWindow::ClearPoints()
{
    m_paths.clear();

    return Render();
}

HRESULT PathWindow::CreateDeviceIndependentResources()
{
    HRESULT hr = S_OK;

    HR(D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, &m_pD2Factory));
    HR(CoCreateInstance(CLSID_WICImagingFactory, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&m_pWICFactory)));
    HR(m_pD2Factory->CreateStrokeStyle(D2D1::StrokeStyleProperties(D2D1_CAP_STYLE_FLAT, D2D1_CAP_STYLE_FLAT, D2D1_CAP_STYLE_FLAT, D2D1_LINE_JOIN_ROUND), nullptr, 0, &m_pStrokeStyle));

    return hr;
}

HRESULT PathWindow::CreateDeviceResources()
{
    HRESULT hr = S_OK;

    if (m_pRenderTarget) return hr;

    RECT rc{};
    GetClientRect(m_hWnd, &rc);
    auto width = rc.right - rc.left;
    auto height = rc.bottom - rc.top;

    ComPtr<IWICBitmap> pBitmap;
    HR(m_pWICFactory->CreateBitmap(width, height, GUID_WICPixelFormat32bppPBGRA, WICBitmapCacheOnLoad, pBitmap.GetAddressOf()));

    D2D1_RENDER_TARGET_PROPERTIES renderTargetProps{};
    renderTargetProps.type = D2D1_RENDER_TARGET_TYPE_DEFAULT;
    renderTargetProps.pixelFormat = D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED);
    renderTargetProps.dpiX = 0.0f;
    renderTargetProps.dpiY = 0.0f;
    renderTargetProps.usage = D2D1_RENDER_TARGET_USAGE_GDI_COMPATIBLE;
    renderTargetProps.minLevel = D2D1_FEATURE_LEVEL_DEFAULT;

    HR(m_pD2Factory->CreateWicBitmapRenderTarget(pBitmap.Get(), renderTargetProps, &m_pRenderTarget));

    m_pRenderTarget->SetAntialiasMode(D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);

    HR(m_pRenderTarget->CreateSolidColorBrush(D2D1::ColorF(D2D1::ColorF::Red, 0.7f), &m_pPathBrush));

    hr = m_pRenderTarget->QueryInterface(&m_pInteropTarget);

    return hr;
}

void PathWindow::DiscardDeviceResources()
{
    SafeRelease(&m_pRenderTarget);
    SafeRelease(&m_pInteropTarget);
    SafeRelease(&m_pPathBrush);
}

HRESULT PathWindow::Render()
{
#ifdef MEASURE_RENDER
    std::chrono::steady_clock::time_point begin = std::chrono::steady_clock::now();
#endif

    HRESULT hr = S_OK;

    HR(CreateDeviceResources());

    m_pRenderTarget->BeginDraw();
    m_pRenderTarget->SetTransform(D2D1::Matrix3x2F::Identity());
    m_pRenderTarget->Clear(CLICKABLE ? D2D1::ColorF(0.0f, 0.0f, 0.0f, 0.1f) : D2D1::ColorF(0.0f, 0.0f, 0.0f, 0.0f));

    //D2D1_SIZE_F rtSize = m_pRenderTarget->GetSize();

    if (m_paths.size() > 0)
    {
#ifdef MEASURE_RENDER
        std::chrono::steady_clock::time_point beginRender = std::chrono::steady_clock::now();
#endif

        ComPtr<ID2D1PathGeometry> pGeometry;
        HR(m_pD2Factory->CreatePathGeometry(pGeometry.GetAddressOf()));

        ComPtr<ID2D1GeometrySink> pSink;
        HR(pGeometry->Open(pSink.GetAddressOf()));

        for (auto&& path : m_paths)
        {
            if (path.size() <= 1) continue;

            pSink->BeginFigure(D2D1::Point2F(static_cast<float>(path[0].x), static_cast<float>(path[0].y)), D2D1_FIGURE_BEGIN_HOLLOW);
            pSink->AddLines(&path[1], static_cast<UINT32>(path.size() - 1));
            pSink->EndFigure(D2D1_FIGURE_END_OPEN);
        }

        HR(pSink->Close());

        m_pRenderTarget->DrawGeometry(pGeometry.Get(), m_pPathBrush, 3.0f, m_pStrokeStyle);

        //for (int i = 1; i < m_points.size(); ++i)
        //{
        //    m_pRenderTarget->DrawLine(m_points[i - 1], m_points[i], m_pPathBrush, 3.0f, m_pStrokeStyle);
        //}

#ifdef MEASURE_RENDER
        std::chrono::steady_clock::time_point endRender = std::chrono::steady_clock::now();
        OutputDebugString(L"Geometry: ");
        OutputDebugString(std::to_wstring(std::chrono::duration_cast<std::chrono::milliseconds>(endRender - beginRender).count()).c_str());
        OutputDebugString(L"ms\n");
#endif
    }

    {
#ifdef MEASURE_RENDER
        std::chrono::steady_clock::time_point beginDC = std::chrono::steady_clock::now();
#endif

        HDC dc;
        HR(m_pInteropTarget->GetDC(D2D1_DC_INITIALIZE_MODE_COPY, &dc));

        hr = m_info.Update(m_hWnd, dc);

        RECT r{};
        m_pInteropTarget->ReleaseDC(&r);

        if (FAILED(hr)) return hr;

#ifdef MEASURE_RENDER
        std::chrono::steady_clock::time_point endDC = std::chrono::steady_clock::now();
        OutputDebugString(L"DC: ");
        OutputDebugString(std::to_wstring(std::chrono::duration_cast<std::chrono::milliseconds>(endDC - beginDC).count()).c_str());
        OutputDebugString(L"ms\n");
#endif
    }

    hr = m_pRenderTarget->EndDraw();
    if (hr == D2DERR_RECREATE_TARGET)
    {
        DiscardDeviceResources();
        hr = S_OK;
    }

#ifdef MEASURE_RENDER
    std::chrono::steady_clock::time_point end = std::chrono::steady_clock::now();
    OutputDebugString(L"All: ");
    OutputDebugString(std::to_wstring(std::chrono::duration_cast<std::chrono::milliseconds>(end - begin).count()).c_str());
    OutputDebugString(L"ms\n\n");
#endif

    return hr;
}

LRESULT CALLBACK PathWindow::WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    if (message == WM_CREATE)
    {
        auto pcs = (LPCREATESTRUCT)lParam;
        auto pPathWindow = (PathWindow*)pcs->lpCreateParams;

        SetWindowLongPtr(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pPathWindow));

        return 1;
    }

    auto pPathWindow = reinterpret_cast<PathWindow*>(static_cast<LONG_PTR>(GetWindowLongPtr(hWnd, GWLP_USERDATA)));

    if (pPathWindow)
    {
        switch (message)
        {
        case WM_SIZE:
            return 0;

        case WM_DISPLAYCHANGE:
            PostQuitMessage(1);
            return 0;

        case WM_PAINT:
            return 0;

        case WM_CLOSE:
            DestroyWindow(pPathWindow->m_hWnd);
            return 0;

        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;

        default:
            if (pPathWindow->m_onUnhandledMsg) pPathWindow->m_onUnhandledMsg(hWnd, message, wParam, lParam);
        }
    }

    return DefWindowProc(hWnd, message, wParam, lParam);
}
