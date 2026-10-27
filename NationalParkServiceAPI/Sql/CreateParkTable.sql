-- Saved as UTF-8 (photo credits contain accented names). With sqlcmd, run: sqlcmd -S .\SQLEXPRESS -d NationalParkService -E -C -f 65001 -i CreateParkTable.sql
IF OBJECT_ID('dbo.Park', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Park
    (
        ParkId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Park PRIMARY KEY,
        ParkName NVARCHAR(100)     NOT NULL
    );
END
GO

-- Detail columns (added to an existing table if this script ran before they existed).
IF COL_LENGTH('dbo.Park', 'State') IS NULL
    ALTER TABLE dbo.Park ADD State NVARCHAR(50) NOT NULL CONSTRAINT DF_Park_State DEFAULT '';
IF COL_LENGTH('dbo.Park', 'Region') IS NULL
    ALTER TABLE dbo.Park ADD Region NVARCHAR(50) NOT NULL CONSTRAINT DF_Park_Region DEFAULT '';
IF COL_LENGTH('dbo.Park', 'Scene') IS NULL
    ALTER TABLE dbo.Park ADD Scene NVARCHAR(20) NOT NULL CONSTRAINT DF_Park_Scene DEFAULT 'peaks';
IF COL_LENGTH('dbo.Park', 'Tagline') IS NULL
    ALTER TABLE dbo.Park ADD Tagline NVARCHAR(200) NOT NULL CONSTRAINT DF_Park_Tagline DEFAULT '';
IF COL_LENGTH('dbo.Park', 'Rating') IS NULL
    ALTER TABLE dbo.Park ADD Rating DECIMAL(2,1) NOT NULL CONSTRAINT DF_Park_Rating DEFAULT 0;
IF COL_LENGTH('dbo.Park', 'Reviews') IS NULL
    ALTER TABLE dbo.Park ADD Reviews INT NOT NULL CONSTRAINT DF_Park_Reviews DEFAULT 0;
IF COL_LENGTH('dbo.Park', 'BaseRate') IS NULL
    ALTER TABLE dbo.Park ADD BaseRate INT NOT NULL CONSTRAINT DF_Park_BaseRate DEFAULT 0;
IF COL_LENGTH('dbo.Park', 'Badge') IS NULL
    ALTER TABLE dbo.Park ADD Badge NVARCHAR(50) NULL;
-- JSON array of activity names, e.g. ["Hiking","Camping"].
IF COL_LENGTH('dbo.Park', 'Activities') IS NULL
    ALTER TABLE dbo.Park ADD Activities NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Park_Activities DEFAULT '[]';
GO

-- SitesLeft was a made-up count; the API now counts free sites from reservations. Drop it if an earlier run added it.
IF COL_LENGTH('dbo.Park', 'SitesLeft') IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.DF_Park_SitesLeft', 'D') IS NOT NULL
        ALTER TABLE dbo.Park DROP CONSTRAINT DF_Park_SitesLeft;
    ALTER TABLE dbo.Park DROP COLUMN SitesLeft;
END
GO

-- All 63 U.S. national parks. Inserts missing parks and refreshes details on existing ones, so this is safe to re-run.
MERGE dbo.Park AS target
USING (VALUES
    ('Acadia', 'Maine', 'East', 'coast', 'First sunrise in the U.S. from Cadillac Mountain.', 4.8, 6987, 30, NULL, '["Kayaking","Hiking","Camping"]'),
    ('American Samoa', 'American Samoa', 'Islands', 'coast', 'Coral reefs and rainforest on the edge of the South Pacific.', 4.6, 412, 20, NULL, '["Hiking","Kayaking","Wildlife"]'),
    ('Arches', 'Utah', 'Southwest', 'mesa', 'Over 2,000 natural stone arches under dark-sky nights.', 4.7, 6120, 25, NULL, '["Stargazing","Hiking","Climbing"]'),
    ('Badlands', 'South Dakota', 'Midwest', 'mesa', 'Striped buttes, fossil beds and roaming bison.', 4.7, 5210, 22, NULL, '["Hiking","Wildlife","Stargazing","Camping"]'),
    ('Big Bend', 'Texas', 'Southwest', 'mesa', 'Desert, river canyons and some of the darkest skies in the country.', 4.8, 4876, 26, NULL, '["Hiking","Stargazing","Kayaking","Camping"]'),
    ('Biscayne', 'Florida', 'East', 'coast', 'A mostly underwater park of reefs, keys and shipwrecks.', 4.5, 2034, 25, NULL, '["Kayaking","Wildlife","Camping"]'),
    ('Black Canyon of the Gunnison', 'Colorado', 'Rockies', 'peaks', 'Sheer dark walls dropping 2,700 feet to the river.', 4.8, 2950, 24, NULL, '["Climbing","Hiking","Stargazing","Camping"]'),
    ('Bryce Canyon', 'Utah', 'Southwest', 'mesa', 'Amphitheaters of orange hoodoos glowing at sunrise.', 4.9, 8432, 30, NULL, '["Hiking","Stargazing","Camping","Lodging"]'),
    ('Canyonlands', 'Utah', 'Southwest', 'mesa', 'Mesas and canyons carved by the Colorado and Green rivers.', 4.8, 4321, 25, NULL, '["Hiking","Kayaking","Stargazing","Camping"]'),
    ('Capitol Reef', 'Utah', 'Southwest', 'mesa', 'Waterpocket Fold cliffs and pie from the historic Fruita orchards.', 4.8, 3987, 25, NULL, '["Hiking","Climbing","Stargazing","Camping"]'),
    ('Carlsbad Caverns', 'New Mexico', 'Southwest', 'mesa', 'A vast limestone cave and nightly bat flights at dusk.', 4.7, 5643, 20, NULL, '["Hiking","Wildlife","Stargazing"]'),
    ('Channel Islands', 'California', 'West', 'coast', 'Five wild islands with sea caves, island foxes and kelp forests.', 4.7, 2210, 15, NULL, '["Kayaking","Hiking","Wildlife","Camping"]'),
    ('Congaree', 'South Carolina', 'East', 'forest', 'Champion trees in the largest old-growth bottomland forest in the Southeast.', 4.5, 1876, 20, NULL, '["Kayaking","Hiking","Camping"]'),
    ('Crater Lake', 'Oregon', 'Pacific NW', 'peaks', 'The deepest lake in the U.S., brilliant blue inside a collapsed volcano.', 4.9, 6011, 26, NULL, '["Hiking","Camping","Lodging"]'),
    ('Cuyahoga Valley', 'Ohio', 'Midwest', 'forest', 'Waterfalls, towpath trails and a scenic railroad.', 4.6, 3402, 20, NULL, '["Hiking","Wildlife","Lodging"]'),
    ('Death Valley', 'California', 'West', 'mesa', 'Salt flats, sand dunes and the lowest point in North America.', 4.7, 7120, 22, NULL, '["Hiking","Stargazing","Camping","Lodging"]'),
    ('Denali', 'Alaska', 'Alaska', 'peaks', 'North America''s tallest peak above six million acres of wild land.', 4.9, 5532, 30, NULL, '["Wildlife","Hiking","Camping"]'),
    ('Dry Tortugas', 'Florida', 'East', 'coast', 'Fort Jefferson and turquoise water 70 miles past Key West.', 4.8, 2687, 15, NULL, '["Kayaking","Wildlife","Camping"]'),
    ('Everglades', 'Florida', 'East', 'coast', 'Sawgrass prairies, mangroves, alligators and manatees.', 4.5, 6234, 25, NULL, '["Kayaking","Wildlife","Camping"]'),
    ('Gates of the Arctic', 'Alaska', 'Alaska', 'peaks', 'No roads or trails, just the Brooks Range above the Arctic Circle.', 4.8, 198, 15, NULL, '["Hiking","Wildlife","Camping"]'),
    ('Gateway Arch', 'Missouri', 'Midwest', 'forest', 'The 630-foot Gateway to the West on the Mississippi River.', 4.6, 6543, 20, NULL, '["Hiking","Lodging"]'),
    ('Glacier', 'Montana', 'Rockies', 'peaks', 'Going-to-the-Sun Road and turquoise alpine lakes.', 4.9, 7655, 30, 'New sites', '["Hiking","Kayaking","Wildlife","Camping"]'),
    ('Glacier Bay', 'Alaska', 'Alaska', 'coast', 'Tidewater glaciers calving into fjords full of whales.', 4.8, 1543, 20, NULL, '["Kayaking","Wildlife","Camping","Lodging"]'),
    ('Grand Canyon', 'Arizona', 'Southwest', 'mesa', 'A mile deep and 277 miles of layered red rock.', 4.8, 15210, 30, NULL, '["Hiking","Camping","Stargazing","Lodging"]'),
    ('Grand Teton', 'Wyoming', 'Rockies', 'peaks', 'Jagged peaks rising straight out of Jackson Hole.', 4.9, 9120, 34, NULL, '["Hiking","Climbing","Kayaking","Wildlife","Camping"]'),
    ('Great Basin', 'Nevada', 'West', 'peaks', 'Ancient bristlecone pines, Lehman Caves and dark night skies.', 4.7, 1654, 20, NULL, '["Hiking","Stargazing","Camping"]'),
    ('Great Sand Dunes', 'Colorado', 'Rockies', 'mesa', 'The tallest dunes in North America below snowy peaks.', 4.7, 4012, 22, NULL, '["Hiking","Stargazing","Camping"]'),
    ('Great Smoky Mountains', 'Tennessee', 'East', 'forest', 'Misty ridges, old-growth forest and fireflies in June.', 4.7, 11320, 30, NULL, '["Hiking","Wildlife","Camping","Lodging"]'),
    ('Guadalupe Mountains', 'Texas', 'Southwest', 'mesa', 'The highest point in Texas atop an ancient fossil reef.', 4.7, 1987, 20, NULL, '["Hiking","Stargazing","Camping"]'),
    ('Haleakala', 'Hawaii', 'Islands', 'peaks', 'Sunrise above the clouds from a 10,000-foot volcanic summit.', 4.8, 5876, 20, NULL, '["Hiking","Stargazing","Camping"]'),
    ('Hawaii Volcanoes', 'Hawaii', 'Islands', 'peaks', 'Kilauea and Mauna Loa, two of the world''s most active volcanoes.', 4.8, 6543, 25, NULL, '["Hiking","Stargazing","Camping","Lodging"]'),
    ('Hot Springs', 'Arkansas', 'East', 'forest', 'Historic Bathhouse Row and thermal springs in the Ouachitas.', 4.6, 3654, 20, NULL, '["Hiking","Camping","Lodging"]'),
    ('Indiana Dunes', 'Indiana', 'Midwest', 'coast', 'Fifteen miles of Lake Michigan beaches and towering dunes.', 4.6, 4123, 25, NULL, '["Hiking","Kayaking","Camping"]'),
    ('Isle Royale', 'Michigan', 'Midwest', 'forest', 'A remote Lake Superior island of wolves and moose.', 4.8, 987, 15, NULL, '["Hiking","Kayaking","Wildlife","Camping","Lodging"]'),
    ('Joshua Tree', 'California', 'West', 'mesa', 'Twisted Joshua trees, granite boulders and desert stars.', 4.8, 9345, 25, NULL, '["Climbing","Hiking","Stargazing","Camping"]'),
    ('Katmai', 'Alaska', 'Alaska', 'coast', 'Brown bears fishing for salmon at Brooks Falls.', 4.9, 876, 20, NULL, '["Wildlife","Hiking","Camping","Lodging"]'),
    ('Kenai Fjords', 'Alaska', 'Alaska', 'coast', 'Harding Icefield glaciers flowing down to the sea.', 4.8, 2109, 20, NULL, '["Kayaking","Hiking","Wildlife"]'),
    ('Kings Canyon', 'California', 'West', 'peaks', 'A canyon deeper than the Grand Canyon and giant sequoia groves.', 4.8, 3456, 32, NULL, '["Hiking","Camping","Lodging"]'),
    ('Kobuk Valley', 'Alaska', 'Alaska', 'mesa', 'Arctic sand dunes crossed by a half-million migrating caribou.', 4.6, 124, 15, NULL, '["Wildlife","Hiking","Camping"]'),
    ('Lake Clark', 'Alaska', 'Alaska', 'peaks', 'Volcanoes, turquoise lakes and bears on the coastal meadows.', 4.8, 342, 20, NULL, '["Wildlife","Kayaking","Hiking","Camping"]'),
    ('Lassen Volcanic', 'California', 'West', 'peaks', 'Boiling mud pots, steaming vents and a volcano you can climb.', 4.8, 3210, 24, NULL, '["Hiking","Kayaking","Stargazing","Camping"]'),
    ('Mammoth Cave', 'Kentucky', 'East', 'forest', 'The longest known cave system in the world.', 4.7, 5987, 25, NULL, '["Hiking","Kayaking","Camping","Lodging"]'),
    ('Mesa Verde', 'Colorado', 'Southwest', 'mesa', 'Ancestral Puebloan cliff dwellings tucked into canyon walls.', 4.8, 4567, 30, NULL, '["Hiking","Camping","Lodging"]'),
    ('Mount Rainier', 'Washington', 'Pacific NW', 'peaks', 'A glacier-clad volcano ringed by wildflower meadows.', 4.9, 8765, 28, NULL, '["Hiking","Climbing","Camping","Lodging"]'),
    ('New River Gorge', 'West Virginia', 'East', 'forest', 'Whitewater, sandstone crags and a famous arch bridge.', 4.7, 3109, 20, 'New sites', '["Kayaking","Climbing","Hiking","Camping"]'),
    ('North Cascades', 'Washington', 'Pacific NW', 'peaks', 'Over 300 glaciers and the turquoise waters of Diablo Lake.', 4.8, 2345, 22, NULL, '["Hiking","Kayaking","Climbing","Camping"]'),
    ('Olympic', 'Washington', 'Pacific NW', 'coast', 'Rainforest, rugged coastline and glacier-capped peaks.', 4.7, 5432, 24, NULL, '["Hiking","Kayaking","Camping","Wildlife"]'),
    ('Petrified Forest', 'Arizona', 'Southwest', 'mesa', 'Rainbow-colored fossil logs and the Painted Desert.', 4.6, 4210, 20, NULL, '["Hiking","Stargazing"]'),
    ('Pinnacles', 'California', 'West', 'mesa', 'Volcanic spires, talus caves and soaring California condors.', 4.6, 2765, 24, NULL, '["Climbing","Hiking","Wildlife","Camping"]'),
    ('Redwood', 'California', 'West', 'forest', 'The tallest trees on Earth along a foggy Pacific coast.', 4.8, 5432, 35, NULL, '["Hiking","Kayaking","Wildlife","Camping"]'),
    ('Rocky Mountain', 'Colorado', 'Rockies', 'peaks', 'Elk meadows and trails above 12,000 feet.', 4.8, 8890, 30, NULL, '["Hiking","Wildlife","Camping","Stargazing"]'),
    ('Saguaro', 'Arizona', 'Southwest', 'mesa', 'Forests of giant saguaro cacti on both sides of Tucson.', 4.7, 3876, 15, NULL, '["Hiking","Stargazing","Wildlife"]'),
    ('Sequoia', 'California', 'West', 'forest', 'The General Sherman Tree and the granite climb up Moro Rock.', 4.9, 7654, 32, NULL, '["Hiking","Climbing","Camping","Lodging"]'),
    ('Shenandoah', 'Virginia', 'East', 'forest', 'Skyline Drive and Blue Ridge views over 500 miles of trails.', 4.8, 7012, 30, NULL, '["Hiking","Wildlife","Camping","Lodging"]'),
    ('Theodore Roosevelt', 'North Dakota', 'Midwest', 'mesa', 'Painted badlands, wild horses and prairie dog towns.', 4.7, 2345, 14, NULL, '["Wildlife","Hiking","Kayaking","Camping"]'),
    ('Virgin Islands', 'U.S. Virgin Islands', 'Islands', 'coast', 'White-sand beaches and snorkel trails on St. John.', 4.7, 2876, 30, NULL, '["Kayaking","Hiking","Camping"]'),
    ('Voyageurs', 'Minnesota', 'Midwest', 'coast', 'Connected lakes, houseboats and the northern lights.', 4.6, 1234, 20, NULL, '["Kayaking","Stargazing","Wildlife","Camping"]'),
    ('White Sands', 'New Mexico', 'Southwest', 'mesa', 'Glistening white gypsum dunes made for sledding.', 4.8, 5234, 15, NULL, '["Hiking","Stargazing","Camping"]'),
    ('Wind Cave', 'South Dakota', 'Midwest', 'forest', 'Boxwork cave formations below a bison-dotted prairie.', 4.6, 2109, 18, NULL, '["Hiking","Wildlife","Camping"]'),
    ('Wrangell-St. Elias', 'Alaska', 'Alaska', 'peaks', 'The largest national park, bigger than Switzerland.', 4.8, 543, 15, NULL, '["Hiking","Climbing","Wildlife","Camping"]'),
    ('Yellowstone', 'Wyoming', 'Rockies', 'forest', 'Geysers, bison herds and the first national park on Earth.', 4.8, 14002, 33, NULL, '["Wildlife","Hiking","Camping","Lodging"]'),
    ('Yosemite', 'California', 'West', 'peaks', 'Granite cliffs, giant sequoias and thundering waterfalls.', 4.9, 12840, 36, 'Most popular', '["Hiking","Camping","Climbing","Lodging"]'),
    ('Zion', 'Utah', 'Southwest', 'mesa', 'Slot canyons, emerald pools and the Angels Landing climb.', 4.9, 9874, 35, NULL, '["Hiking","Climbing","Camping"]')
) AS source (ParkName, State, Region, Scene, Tagline, Rating, Reviews, BaseRate, Badge, Activities)
ON target.ParkName = source.ParkName
WHEN MATCHED THEN UPDATE SET
    State = source.State,
    Region = source.Region,
    Scene = source.Scene,
    Tagline = source.Tagline,
    Rating = source.Rating,
    Reviews = source.Reviews,
    BaseRate = source.BaseRate,
    Badge = source.Badge,
    Activities = source.Activities
WHEN NOT MATCHED THEN
    INSERT (ParkName, State, Region, Scene, Tagline, Rating, Reviews, BaseRate, Badge, Activities)
    VALUES (source.ParkName, source.State, source.Region, source.Scene, source.Tagline, source.Rating,
            source.Reviews, source.BaseRate, source.Badge, source.Activities);

GO

-- Photo columns. Photos are freely licensed images from Wikimedia Commons; the credit must be shown with the photo.
IF COL_LENGTH('dbo.Park', 'ImageUrl') IS NULL
    ALTER TABLE dbo.Park ADD ImageUrl NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.Park', 'ImageCredit') IS NULL
    ALTER TABLE dbo.Park ADD ImageCredit NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Park', 'ImageSourceUrl') IS NULL
    ALTER TABLE dbo.Park ADD ImageSourceUrl NVARCHAR(500) NULL;
GO

UPDATE p
SET ImageUrl = v.ImageUrl,
    ImageCredit = v.ImageCredit,
    ImageSourceUrl = v.ImageSourceUrl
FROM dbo.Park AS p
JOIN (VALUES
    (N'Acadia', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/e/e9/Acadia_National_Park_02.JPG/960px-Acadia_National_Park_02.JPG', N'Plh1234us, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Acadia_National_Park_02.JPG'),
    (N'American Samoa', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/3/3c/Pola_Islands_Tutuila_NPS.jpg/960px-Pola_Islands_Tutuila_NPS.jpg', N'Tavita Togia, National Park Service, Public domain', N'https://commons.wikimedia.org/wiki/File:Pola_Islands_Tutuila_NPS.jpg'),
    (N'Arches', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/f0/Delicate_arch_sunset.jpg/960px-Delicate_arch_sunset.jpg', N'Palacemusic, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Delicate_arch_sunset.jpg'),
    (N'Badlands', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/b/b9/MK00609_Badlands.jpg/960px-MK00609_Badlands.jpg', N'Martin Kraft, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:MK00609_Badlands.jpg'),
    (N'Big Bend', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/f6/Canyon%2C_Rio_Grande%2C_Texas.jpeg/960px-Canyon%2C_Rio_Grande%2C_Texas.jpeg', N'Michael from Minnesota, CC BY-SA 2.0', N'https://commons.wikimedia.org/wiki/File:Canyon,_Rio_Grande,_Texas.jpeg'),
    (N'Biscayne', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/4/48/Biscayne.JPG/960px-Biscayne.JPG', N'Catholic 85 at English Wikipedia, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Biscayne.JPG'),
    (N'Black Canyon of the Gunnison', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/0/0b/Black_Canyon_and_Gunnison_River_2008.jpg/960px-Black_Canyon_and_Gunnison_River_2008.jpg', N'Terry Foote, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Black_Canyon_and_Gunnison_River_2008.jpg'),
    (N'Bryce Canyon', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/c/c3/Inspiration_Point_Bryce_Canyon_November_2018_panorama.jpg/960px-Inspiration_Point_Bryce_Canyon_November_2018_panorama.jpg', N'King of Hearts, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Inspiration_Point_Bryce_Canyon_November_2018_panorama.jpg'),
    (N'Canyonlands', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/9/99/Green_River_Overlook_Ekker_Butte.jpg/960px-Green_River_Overlook_Ekker_Butte.jpg', N'au_ears, CC BY-SA 2.0', N'https://commons.wikimedia.org/wiki/File:Green_River_Overlook_Ekker_Butte.jpg'),
    (N'Capitol Reef', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/1/1f/Capitol_Reef_National_Park.jpg/960px-Capitol_Reef_National_Park.jpg', N'Wolfgang Staudt, CC BY 2.0', N'https://commons.wikimedia.org/wiki/File:Capitol_Reef_National_Park.jpg'),
    (N'Carlsbad Caverns', N'https://upload.wikimedia.org/wikipedia/commons/d/dd/Carlsbad_Interior_Formations.jpg', N'Eric Guinther, User:Marshman, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Carlsbad_Interior_Formations.jpg'),
    (N'Channel Islands', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/c/c7/Anacapa_Island%2C_Arch_Rock.jpg/960px-Anacapa_Island%2C_Arch_Rock.jpg', N'Craig Baker, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Anacapa_Island,_Arch_Rock.jpg'),
    (N'Congaree', N'https://upload.wikimedia.org/wikipedia/commons/4/4f/A548%2C_Congaree_National_Park%2C_South_Carolina%2C_USA%2C_2012.jpg', N'Brian W. Schaller, FAL', N'https://commons.wikimedia.org/wiki/File:A548,_Congaree_National_Park,_South_Carolina,_USA,_2012.jpg'),
    (N'Crater Lake', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/8d/Above_Crater_Lake_%28cropped%29.jpg/960px-Above_Crater_Lake_%28cropped%29.jpg', N'Athleticamps, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Above_Crater_Lake_(cropped).jpg'),
    (N'Cuyahoga Valley', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/a4/Cuyahoga_Valley_National_Park_20.jpg/960px-Cuyahoga_Valley_National_Park_20.jpg', N'Niagara66, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Cuyahoga_Valley_National_Park_20.jpg'),
    (N'Death Valley', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/87/Mesquite_Sand_Dunes_in_Death_Valley.jpg/960px-Mesquite_Sand_Dunes_in_Death_Valley.jpg', N'Brocken Inaglory, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Mesquite_Sand_Dunes_in_Death_Valley.jpg'),
    (N'Denali', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/c/c6/Every_Road-_Denali_%287945497984%29.jpg/960px-Every_Road-_Denali_%287945497984%29.jpg', N'Denali National Park and Preserve, Public domain', N'https://commons.wikimedia.org/wiki/File:Every_Road-_Denali_(7945497984).jpg'),
    (N'Dry Tortugas', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/5f/Fort-Jefferson_Dry-Tortugas.jpg/960px-Fort-Jefferson_Dry-Tortugas.jpg', N'U.S. National Park Service, Public domain', N'https://commons.wikimedia.org/wiki/File:Fort-Jefferson_Dry-Tortugas.jpg'),
    (N'Everglades', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/2a/Sunset_over_the_River_of_Grass%2C_NPSphoto%2C_G.Gardner_%289255157507%29.jpg/960px-Sunset_over_the_River_of_Grass%2C_NPSphoto%2C_G.Gardner_%289255157507%29.jpg', N'Everglades NPS, Public domain', N'https://commons.wikimedia.org/wiki/File:Sunset_over_the_River_of_Grass,_NPSphoto,_G.Gardner_(9255157507).jpg'),
    (N'Gates of the Arctic', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/fc/Remote_river_in_Gates_of_the_Arctic_%2816524035298%29.jpg/960px-Remote_river_in_Gates_of_the_Arctic_%2816524035298%29.jpg', N'Sean Tevebaugh, National Park Service, Public domain', N'https://commons.wikimedia.org/wiki/File:Remote_river_in_Gates_of_the_Arctic_(16524035298).jpg'),
    (N'Gateway Arch', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/d/de/Gateway_Arch%2C_St._Louis.jpg/960px-Gateway_Arch%2C_St._Louis.jpg', N'Lewis Hulbert, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Gateway_Arch,_St._Louis.jpg'),
    (N'Glacier', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/51/Mountain_Goat_at_Hidden_Lake.jpg/960px-Mountain_Goat_at_Hidden_Lake.jpg', N'Robert M. Russell, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Mountain_Goat_at_Hidden_Lake.jpg'),
    (N'Glacier Bay', N'https://upload.wikimedia.org/wikipedia/commons/b/bf/A045%2C_Glacier_Bay_National_Park%2C_Alaska%2C_USA%2C_Johns_Hopkins_Glacier%2C_2002.jpg', N'Brian W. Schaller, FAL', N'https://commons.wikimedia.org/wiki/File:A045,_Glacier_Bay_National_Park,_Alaska,_USA,_Johns_Hopkins_Glacier,_2002.jpg'),
    (N'Grand Canyon', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/aa/Dawn_on_the_S_rim_of_the_Grand_Canyon_%288645178272%29.jpg/960px-Dawn_on_the_S_rim_of_the_Grand_Canyon_%288645178272%29.jpg', N'Murray Foubister, CC BY-SA 2.0', N'https://commons.wikimedia.org/wiki/File:Dawn_on_the_S_rim_of_the_Grand_Canyon_(8645178272).jpg'),
    (N'Grand Teton', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/d/d0/Barns_grand_tetons.jpg/960px-Barns_grand_tetons.jpg', N'Jon Sullivan, Public domain', N'https://commons.wikimedia.org/wiki/File:Barns_grand_tetons.jpg'),
    (N'Great Basin', N'https://upload.wikimedia.org/wikipedia/commons/b/be/Prometheus_Wheeler.jpg', N'James R Bouldin, Public domain', N'https://commons.wikimedia.org/wiki/File:Prometheus_Wheeler.jpg'),
    (N'Great Sand Dunes', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/4/48/Great_Sand_Dunes_National_Park_and_Preserve%2C_United_States_%28Unsplash%29.jpg/960px-Great_Sand_Dunes_National_Park_and_Preserve%2C_United_States_%28Unsplash%29.jpg', N'Lionello DelPiccolo liodp, CC0', N'https://commons.wikimedia.org/wiki/File:Great_Sand_Dunes_National_Park_and_Preserve,_United_States_(Unsplash).jpg'),
    (N'Great Smoky Mountains', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/b/bc/View_atop_Cliff_Tops_on_Mount_LeConte%2C_GSMNP%2C_TN.jpg/960px-View_atop_Cliff_Tops_on_Mount_LeConte%2C_GSMNP%2C_TN.jpg', N'AppalachianCentrist, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:View_atop_Cliff_Tops_on_Mount_LeConte,_GSMNP,_TN.jpg'),
    (N'Guadalupe Mountains', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/8f/Guadalupe_Peak_from_Hunter_Peak.jpg/960px-Guadalupe_Peak_from_Hunter_Peak.jpg', N'CarlCarlsonIV, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Guadalupe_Peak_from_Hunter_Peak.jpg'),
    (N'Haleakala', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/4/48/Haleakala_National_Park_02.jpg/960px-Haleakala_National_Park_02.jpg', N'Niagara66, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Haleakala_National_Park_02.jpg'),
    (N'Hawaii Volcanoes', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/89/P%C4%81hoehoe_and_Aa_flows_at_Hawaii.jpg/960px-P%C4%81hoehoe_and_Aa_flows_at_Hawaii.jpg', N'Brocken Inaglory, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:P%C4%81hoehoe_and_Aa_flows_at_Hawaii.jpg'),
    (N'Hot Springs', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/a1/Steamy_Entrance_Springs_on_Cold_Night_in_Hot_Springs_National_Park.jpg/960px-Steamy_Entrance_Springs_on_Cold_Night_in_Hot_Springs_National_Park.jpg', N'TripOfALifestyle.com, CC BY 4.0', N'https://commons.wikimedia.org/wiki/File:Steamy_Entrance_Springs_on_Cold_Night_in_Hot_Springs_National_Park.jpg'),
    (N'Indiana Dunes', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/3/3d/Indiana_Dunes_National_Lakeshore%2C_Michigan_City%2C_Indiana%2C_Estados_Unidos%2C_2012-10-20%2C_DD_03.jpg/960px-Indiana_Dunes_National_Lakeshore%2C_Michigan_City%2C_Indiana%2C_Estados_Unidos%2C_2012-10-20%2C_DD_03.jpg', N'Diego Delso, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Indiana_Dunes_National_Lakeshore,_Michigan_City,_Indiana,_Estados_Unidos,_2012-10-20,_DD_03.jpg'),
    (N'Isle Royale', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/0/05/Lake_Superior_Shore_at_Isle_Royale_National_Park_%2849051653101%29.jpg/960px-Lake_Superior_Shore_at_Isle_Royale_National_Park_%2849051653101%29.jpg', N'Tony Webster, CC BY 2.0', N'https://commons.wikimedia.org/wiki/File:Lake_Superior_Shore_at_Isle_Royale_National_Park_(49051653101).jpg'),
    (N'Joshua Tree', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/f9/Joshua_Tree_-_Cyclops_%2B_Potato_Head_-_Sunrise.jpg/960px-Joshua_Tree_-_Cyclops_%2B_Potato_Head_-_Sunrise.jpg', N'Jarek Tuszyński, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Joshua_Tree_-_Cyclops_%2B_Potato_Head_-_Sunrise.jpg'),
    (N'Katmai', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/9/98/Fishing_Brown_Bear_at_Brooks_Falls%2C_Katmai_National_Park_%2810106346915%29.jpg/960px-Fishing_Brown_Bear_at_Brooks_Falls%2C_Katmai_National_Park_%2810106346915%29.jpg', N'Christoph Strässler, CC BY-SA 2.0', N'https://commons.wikimedia.org/wiki/File:Fishing_Brown_Bear_at_Brooks_Falls,_Katmai_National_Park_(10106346915).jpg'),
    (N'Kenai Fjords', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/c/cd/Kenai_Fjords_coast.jpg/960px-Kenai_Fjords_coast.jpg', N'NPS, Public domain', N'https://commons.wikimedia.org/wiki/File:Kenai_Fjords_coast.jpg'),
    (N'Kings Canyon', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/3/3a/KingsCanyonNP.JPG/960px-KingsCanyonNP.JPG', N'Crd637, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:KingsCanyonNP.JPG'),
    (N'Kobuk Valley', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/d/de/Kobuk_Bendlova_235.jpg/960px-Kobuk_Bendlova_235.jpg', N'16Terezka, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Kobuk_Bendlova_235.jpg'),
    (N'Lake Clark', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/c/c1/View_at_Lake_Clark_National_Park_and_Preserve%2C_Alaska_%2828aa9caf-b197-4397-82fd-e5550f096383%29.jpg/960px-View_at_Lake_Clark_National_Park_and_Preserve%2C_Alaska_%2828aa9caf-b197-4397-82fd-e5550f096383%29.jpg', N'NPS staff, Public domain', N'https://commons.wikimedia.org/wiki/File:View_at_Lake_Clark_National_Park_and_Preserve,_Alaska_(28aa9caf-b197-4397-82fd-e5550f096383).jpg'),
    (N'Lassen Volcanic', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/ff/Lassen_Peak_and_Lake_Helen.jpg/960px-Lassen_Peak_and_Lake_Helen.jpg', N'NPS Photo, Public domain', N'https://commons.wikimedia.org/wiki/File:Lassen_Peak_and_Lake_Helen.jpg'),
    (N'Mammoth Cave', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/1/19/Mammoth_Cave_National_Park_01.jpg/960px-Mammoth_Cave_National_Park_01.jpg', N'Niagara66, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Mammoth_Cave_National_Park_01.jpg'),
    (N'Mesa Verde', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/83/Cliff_Palace-Colorado-Mesa_Verde_NP.jpg/960px-Cliff_Palace-Colorado-Mesa_Verde_NP.jpg', N'Tobi 87, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Cliff_Palace-Colorado-Mesa_Verde_NP.jpg'),
    (N'Mount Rainier', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/3/32/Mount_Rainier_from_above_Myrtle_Falls_in_August.JPG/960px-Mount_Rainier_from_above_Myrtle_Falls_in_August.JPG', N'Samuel Kerr, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Mount_Rainier_from_above_Myrtle_Falls_in_August.JPG'),
    (N'New River Gorge', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/23/Grandview_Overlook%2C_New_River.jpg/960px-Grandview_Overlook%2C_New_River.jpg', N'Aparkswv, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Grandview_Overlook,_New_River.jpg'),
    (N'North Cascades', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/80/Cascade_Pass_and_Pelton_Basin.jpg/960px-Cascade_Pass_and_Pelton_Basin.jpg', N'National Park Service/Deby Dixon, Public domain', N'https://commons.wikimedia.org/wiki/File:Cascade_Pass_and_Pelton_Basin.jpg'),
    (N'Olympic', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/a8/Cedar_Creek_Abbey_Island_Ruby_Beach.jpg/960px-Cedar_Creek_Abbey_Island_Ruby_Beach.jpg', N'Adbar, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Cedar_Creek_Abbey_Island_Ruby_Beach.jpg'),
    (N'Petrified Forest', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/51/Jasper_Forest_at_Petrified_Forest_NP_in_AZ_12.jpg/960px-Jasper_Forest_at_Petrified_Forest_NP_in_AZ_12.jpg', N'Jeffhollett, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Jasper_Forest_at_Petrified_Forest_NP_in_AZ_12.jpg'),
    (N'Pinnacles', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/4/44/Rock_formations_at_Pinnacles_National_Park.jpg/960px-Rock_formations_at_Pinnacles_National_Park.jpg', N'Brocken Inaglory, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Rock_formations_at_Pinnacles_National_Park.jpg'),
    (N'Redwood', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/d/de/Redwood_National_Park%2C_fog_in_the_forest.jpg/960px-Redwood_National_Park%2C_fog_in_the_forest.jpg', N'Michael Schweppe, CC BY-SA 2.0', N'https://commons.wikimedia.org/wiki/File:Redwood_National_Park,_fog_in_the_forest.jpg'),
    (N'Rocky Mountain', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/3/3e/Rocky_Mountain_National_Park_in_September_2011_-_Glacier_Gorge_from_Bear_Lake.JPG/960px-Rocky_Mountain_National_Park_in_September_2011_-_Glacier_Gorge_from_Bear_Lake.JPG', N'Daniel Mayer (Mav), CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Rocky_Mountain_National_Park_in_September_2011_-_Glacier_Gorge_from_Bear_Lake.JPG'),
    (N'Saguaro', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/b/b4/Saguaro_Sunset.jpg/960px-Saguaro_Sunset.jpg', N'Saguaro Pictures, CC BY 3.0', N'https://commons.wikimedia.org/wiki/File:Saguaro_Sunset.jpg'),
    (N'Sequoia', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/d/d0/General_Sherman_Tree_in_Sequoia_National_Park_-_June_2022.jpg/960px-General_Sherman_Tree_in_Sequoia_National_Park_-_June_2022.jpg', N'Marty Aligata, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:General_Sherman_Tree_in_Sequoia_National_Park_-_June_2022.jpg'),
    (N'Shenandoah', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/1/10/Skyline_Drive_in_the_Fall_%2821852619608%29.jpg/960px-Skyline_Drive_in_the_Fall_%2821852619608%29.jpg', N'Shenandoah National Park, Public domain', N'https://commons.wikimedia.org/wiki/File:Skyline_Drive_in_the_Fall_(21852619608).jpg'),
    (N'Theodore Roosevelt', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/3/34/View_of_Theodore_Roosevelt_National_Park.jpg/960px-View_of_Theodore_Roosevelt_National_Park.jpg', N'NPS/Mark Hoffman, Public domain', N'https://commons.wikimedia.org/wiki/File:View_of_Theodore_Roosevelt_National_Park.jpg'),
    (N'Virgin Islands', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/20/St_John_Trunk_Bay_3.jpg/960px-St_John_Trunk_Bay_3.jpg', N'Fred Hsu, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:St_John_Trunk_Bay_3.jpg'),
    (N'Voyageurs', N'https://upload.wikimedia.org/wikipedia/commons/b/bd/Voyageurs_National_Park.jpg', N'National Park Service, Public domain', N'https://commons.wikimedia.org/wiki/File:Voyageurs_National_Park.jpg'),
    (N'White Sands', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/9/94/Yardangs_in_dunes%2C_White_Sands_National_Park%2C_New_Mexico%2C_United_States.jpg/960px-Yardangs_in_dunes%2C_White_Sands_National_Park%2C_New_Mexico%2C_United_States.jpg', N'Ken Redeker, Public domain', N'https://commons.wikimedia.org/wiki/File:Yardangs_in_dunes,_White_Sands_National_Park,_New_Mexico,_United_States.jpg'),
    (N'Wind Cave', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/1/1c/Skyway_Lake%2C_stalactites%2C_Wind_Cave.jpg/960px-Skyway_Lake%2C_stalactites%2C_Wind_Cave.jpg', N'National Park Service, Public domain', N'https://commons.wikimedia.org/wiki/File:Skyway_Lake,_stalactites,_Wind_Cave.jpg'),
    (N'Wrangell-St. Elias', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/1/19/Wrangell-St.-Elias-Nationalpark_in_Alaska_mid_May.jpg/960px-Wrangell-St.-Elias-Nationalpark_in_Alaska_mid_May.jpg', N'Islander61, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Wrangell-St.-Elias-Nationalpark_in_Alaska_mid_May.jpg'),
    (N'Yellowstone', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/6/60/Grand_Canyon_of_the_Yellowstone_Lower_Falls_%2895819p%29.jpg/960px-Grand_Canyon_of_the_Yellowstone_Lower_Falls_%2895819p%29.jpg', N'Rhododendrites, CC BY-SA 4.0', N'https://commons.wikimedia.org/wiki/File:Grand_Canyon_of_the_Yellowstone_Lower_Falls_(95819p).jpg'),
    (N'Yosemite', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/e/ea/Half_Dome_with_Eastern_Yosemite_Valley_%2850MP%29.jpg/960px-Half_Dome_with_Eastern_Yosemite_Valley_%2850MP%29.jpg', N'Thomas Wolf, www.foto-tw.de, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Half_Dome_with_Eastern_Yosemite_Valley_(50MP).jpg'),
    (N'Zion', N'https://thumb.wikimedia.org/wikipedia/commons/thumb/1/10/Zion_angels_landing_view.jpg/960px-Zion_angels_landing_view.jpg', N'Diliff, CC BY-SA 3.0', N'https://commons.wikimedia.org/wiki/File:Zion_angels_landing_view.jpg')
) AS v (ParkName, ImageUrl, ImageCredit, ImageSourceUrl)
    ON p.ParkName = v.ParkName;
