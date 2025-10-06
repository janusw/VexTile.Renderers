using SQLite;
using VexTile.Common.Enums;
using VexTile.Data.Sources;
using VexTile.Renderer.Mvt.AliFlux;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderers.Mvt.AliFlux.Tests;

public class RenderTest
{
    // PNG header = 137 80 78 71 13 10 26 10
    private static bool IsPng(byte[] bytes) => bytes is [137, 80, 78, 71, 13, 10, 26, 10, ..];

    [Fact]
    public async Task BasicFactoryRenderTest()
    {
        var canvas = new SkiaCanvas();
        var style = new VectorStyle(VectorStyleKind.Default);

        string path = "zurich.mbtiles";
        Assert.True(File.Exists(path));

        SQLiteConnectionString val = new(path, SQLiteOpenFlags.ReadOnly, false);
        var dataSource = new SqliteDataSource(val);
        var provider = new VectorTilesSource(dataSource);
        style.SetSourceProvider("openmaptiles", provider);
        await TileRendererFactory.RenderAsync(style, canvas, new TileInfo(0, 0, 0));
        var tile = canvas.ToPngByteArray();

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("test1.png"))
        {
            File.Delete("test1.png");
        }

        await File.WriteAllBytesAsync("test1.png", tile);
    }

    [Fact]
    public async Task BasicFactoryRenderTestPbf()
    {
        var canvas = new SkiaCanvas();
        var style = new VectorStyle(VectorStyleKind.Default);

        string path = "newyork-mapbox.pbf";
        Assert.True(File.Exists(path));

        var bytes = await File.ReadAllBytesAsync(path);

        var provider = new PbfTileSource(bytes);
        style.SetSourceProvider("openmaptiles", provider);
        await TileRendererFactory.RenderAsync(style, canvas, new TileInfo(0));
        var tile = canvas.ToPngByteArray();

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("test1pbf.png"))
        {
            File.Delete("test1pbf.png");
        }

        await File.WriteAllBytesAsync("test1pbf.png", tile);
    }

    [Fact]
    public async Task BasicTileRendererTest()
    {
        var canvas = new SkiaCanvas();

        string path = "zurich.mbtiles";
        Assert.True(File.Exists(path));

        var dataSource = new SqliteDataSource(path);
        var renderer = new TileRenderer(dataSource, VectorStyleKind.Default);

        var tile = await renderer.RenderTileAsync(canvas, new (0, 0, 0));

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("test2.png"))
        {
            File.Delete("test2.png");
        }

        await File.WriteAllBytesAsync("test2.png", tile);
    }


    [Fact]
    public async Task BasicFactoryRenderTest_WaterOnly()
    {
        var canvas = new SkiaCanvas();
        var style = new VectorStyle(VectorStyleKind.Default);

        string path = "zurich.mbtiles";
        Assert.True(File.Exists(path));

        SQLiteConnectionString val = new(path, SQLiteOpenFlags.ReadOnly, false);
        var dataSource = new SqliteDataSource(val);
        var provider = new VectorTilesSource(dataSource);
        style.SetSourceProvider("openmaptiles", provider);

        var info = new TileInfo(3, 1, 2, layerWhiteList:["water"]); // australia
        await TileRendererFactory.RenderAsync(style, canvas, info);
        var tile = canvas.ToPngByteArray();

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("wateronly.png"))
        {
            File.Delete("wateronly.png");
        }

        await File.WriteAllBytesAsync("wateronly.png", tile);
    }

    [Fact]
    public async Task StyleTest_BaseMap_de()
    {
        // load style
        string url = "https://sgx.geodatenzentrum.de/gdz_basemapde_vektor/styles/bm_web_col.json";
        string json = await new HttpClient().GetStringAsync(url);
        VectorStyle style = new VectorStyle(VectorStyleKind.Custom, customStyle: json);
        Assert.True(style != null);

        // check layers
        Assert.Equal(544, style.Layers.Count);
        foreach (var l in style.Layers)
        {
            if (l.Filter.Count() > 0)
                Console.WriteLine($"{l.Index,3} {l.ID,-70} {l.SourceLayer,-25} {l.Type,-15} {l.Filter.Count()} {l.Filter[0]}");
            else
                Console.WriteLine($"{l.Index,3} {l.ID,-70} {l.SourceLayer,-25} {l.Type,-15} {l.Filter.Count()}");
        }

        // check source
        Assert.Single(style.Sources);
        Source src = style.Sources.First().Value;
        Assert.Equal("smarttiles_de", src.Name);
        Assert.Equal("vector", src.Type);
        Console.WriteLine(src.URL);
        Assert.True(src.Provider != null);    // this fails!

        // test rendering
        var canvas = new SkiaCanvas();
        await TileRendererFactory.RenderAsync(style, canvas, new TileInfo(0));
        var tile = canvas.ToPngByteArray();
        await File.WriteAllBytesAsync("basemap.png", tile);
    }
}