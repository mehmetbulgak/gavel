namespace Gavel.Simulator;

internal static class Catalog
{
    public static readonly IReadOnlyList<int> Durations = [5, 15, 15, 30, 30, 60, 60, 360];

    public static readonly IReadOnlyList<CatalogItem> Items =
    [
        new("Leica M6 rangefinder, 1986, serviced", "Electronics", 42_000m, 55_000m, "Shutter and meter checked last month. Light brassing on the top plate, clean viewfinder."),
        new("Sony WH-1000XM5 headphones, black", "Electronics", 4_500m, null, "Used for three months, original case and cable, battery health excellent."),
        new("Nintendo Game Boy Color, boxed", "Toys", 2_200m, null, "Atomic purple, box and manual included. Screen has no scratches."),
        new("Mechanical keyboard, hot-swap, brown switches", "Electronics", 1_800m, null, "75% layout, PBT keycaps, USB-C. Comes with a spare set of switches."),
        new("Omega Seamaster 300M, 2019", "Watches", 95_000m, 120_000m, "Full set with box and papers. Serviced in 2025, keeps excellent time."),
        new("Seiko Presage cocktail time", "Watches", 9_500m, null, "Sky blue dial, worn on special occasions only. Bracelet unsized."),
        new("Casio G-Shock DW-5600, first owner", "Watches", 1_200m, null, "Daily beater with honest wear. New battery fitted in October."),
        new("Fender Stratocaster, Mexico, 2015", "Music", 18_000m, 22_000m, "Sunburst finish, recently set up with new strings. Hard case included."),
        new("Technics SL-1200 MK2 turntable", "Music", 21_000m, null, "Classic direct drive deck, pitch control works smoothly. Dust cover has small marks."),
        new("Signed first-press vinyl, 1979", "Music", 3_500m, null, "Signature on the sleeve, record plays with minimal surface noise."),
        new("Ottoman copper coffee set", "Home", 2_400m, null, "Hand hammered tray, two cups and a pot. Polished and ready to use."),
        new("Danish teak sideboard, 1960s", "Home", 26_000m, 30_000m, "Four sliding doors, original handles. Pick-up in Kadıköy only."),
        new("Hand-knotted Anatolian kilim, 2 x 3 m", "Home", 12_000m, null, "Natural dyes, professionally cleaned. Small repair on one corner."),
        new("Espresso machine with grinder", "Home", 7_800m, null, "Descaled monthly, pressure gauge works. Includes tamper and milk jug."),
        new("Original oil painting, Bosphorus at dusk", "Art", 6_500m, 9_000m, "Signed by a local artist, 60 x 80 cm, framed in walnut."),
        new("Limited screen print, 12/50", "Art", 1_900m, null, "Numbered and signed, unframed, stored flat since purchase."),
        new("Ceramic vase, İznik style", "Art", 2_800m, null, "Hand painted tulips and carnations, 35 cm tall, no chips."),
        new("1958 Turkish stamp collection", "Collectibles", 4_200m, null, "Two albums, mostly mint. Includes a few rare overprints."),
        new("Vintage fountain pen, gold nib", "Collectibles", 2_600m, null, "Piston filler, writes smoothly with a fine line. Original box."),
        new("Football shirt signed by the 2000 squad", "Sports", 8_000m, null, "Framed with certificate of authenticity. Kept out of sunlight."),
        new("Road bike, carbon frame, size 54", "Sports", 28_000m, 34_000m, "Shimano 105 groupset, tubeless wheels, about 4,000 km ridden."),
        new("Climbing shoes and harness set", "Sports", 1_600m, null, "Shoes in EU 42, harness size M. Both used for one season indoors."),
        new("First edition Turkish novel, 1960", "Books", 3_100m, null, "Cloth binding with dust jacket. Some foxing on the first pages."),
        new("Complete fantasy box set, hardcover", "Books", 1_400m, null, "Seven volumes in slipcase, read once, spines uncracked."),
        new("LEGO architecture set, sealed", "Toys", 2_900m, null, "Retired set, box has minor shelf wear. Never opened."),
        new("Wooden chess set with board", "Toys", 1_100m, null, "Weighted pieces, 50 cm board. One knight has a small dent."),
        new("Leather weekend bag, cognac", "Fashion", 3_600m, null, "Full grain leather, brass hardware, barely used."),
        new("Wool overcoat, size 50", "Fashion", 2_300m, null, "Charcoal, dry cleaned, fits true to size."),
        new("Silk scarf, vintage print", "Fashion", 900m, null, "90 x 90 cm, hand rolled edges, no stains."),
        new("Mirrorless camera body, 24 MP", "Electronics", 16_000m, null, "Shutter count around 12,000. Two batteries and a charger included."),
    ];
}
