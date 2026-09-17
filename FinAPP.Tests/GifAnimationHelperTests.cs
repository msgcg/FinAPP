using System;
using System.Text;
using FinAPP.Services;
using Xunit;

namespace FinAPP.Tests;

public class GifAnimationHelperTests
{
    [Fact]
    public void CreateUniqueGifBytes_AppendsCommentExtensionBeforeTrailer()
    {
        // Создаем имитацию валидного хвоста GIF: байты данных + 0x00 + 0x3B
        byte[] fakeGif = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x10, 0x00, 0x10, 0x00, 0x00, 0x3B };
        
        byte[] patched = GifAnimationHelper.CreateUniqueGifBytes(fakeGif);

        Assert.NotNull(patched);
        Assert.Equal(fakeGif.Length + 12, patched.Length);

        // Проверяем последние байты
        Assert.Equal(0x3B, patched[^1]); // GIF trailer
        Assert.Equal(0x00, patched[^2]); // Block terminator
        Assert.Equal(0x08, patched[^11]); // Sub-block length
        Assert.Equal(0xFE, patched[^12]); // Comment label
        Assert.Equal(0x21, patched[^13]); // Extension introducer
    }

    [Fact]
    public void CreateUniqueGifBytes_TwoCallsProduceDifferentBase64()
    {
        byte[] fakeGif = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x10, 0x00, 0x10, 0x00, 0x00, 0x3B };

        byte[] patched1 = GifAnimationHelper.CreateUniqueGifBytes(fakeGif);
        byte[] patched2 = GifAnimationHelper.CreateUniqueGifBytes(fakeGif);

        string b64_1 = Convert.ToBase64String(patched1);
        string b64_2 = Convert.ToBase64String(patched2);

        Assert.NotEqual(b64_1, b64_2);
    }

    [Fact]
    public void GenerateFinnyHtml_ProducesHtmlWithCorrectBase64()
    {
        string fakeBase64 = "QUJDREVGR0g=";
        string html = GifAnimationHelper.GenerateFinnyHtml(fakeBase64);

        Assert.Contains($"data:image/gif;base64,{fakeBase64}", html);
        Assert.Contains("justify-content: flex-start", html);
        Assert.Contains("background: transparent", html);
    }
}
