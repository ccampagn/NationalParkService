-- Saved as UTF-8 (some names have accents). With sqlcmd, run: sqlcmd -S .\SQLEXPRESS -d NationalParkService -E -C -f 65001 -i AddCampgroundSeason.sql
-- Adds each campground's season so it can't be booked in the off season. Run CreateCampgroundTable.sql first. Safe to re-run.
-- SeasonOpens and SeasonCloses are the first and last nights you can stay, as 'MM-dd'. A season can run across New Year
-- (e.g. '10-15' to '04-15' for a winter desert campground). NULL in both means open year-round.
IF COL_LENGTH('dbo.Campground', 'SeasonOpens') IS NULL
BEGIN
    ALTER TABLE dbo.Campground
        ADD SeasonOpens  CHAR(5) NULL,
            SeasonCloses CHAR(5) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Campground_Season')
BEGIN
    ALTER TABLE dbo.Campground ADD CONSTRAINT CK_Campground_Season CHECK (
        (SeasonOpens IS NULL AND SeasonCloses IS NULL)
        OR (SeasonOpens LIKE '[01][0-9]-[0-3][0-9]' AND SeasonCloses LIKE '[01][0-9]-[0-3][0-9]'));
END
GO

-- Typical seasons for campgrounds that close part of the year; campgrounds not listed stay open year-round.
-- Dates shift a little each year with snow and staffing, so check them against nps.gov and adjust as needed.
UPDATE c
SET c.SeasonOpens = v.SeasonOpens, c.SeasonCloses = v.SeasonCloses
FROM (VALUES
    (N'Acadia', N'Blackwoods', '05-01', '10-31'),
    (N'Acadia', N'Seawall', '05-22', '10-14'),
    (N'Acadia', N'Schoodic Woods', '05-22', '10-14'),
    (N'Acadia', N'Duck Harbor', '05-15', '10-15'),
    (N'Badlands', N'Cedar Pass', '04-15', '10-14'),
    (N'Black Canyon of the Gunnison', N'North Rim', '05-15', '09-30'),
    (N'Black Canyon of the Gunnison', N'East Portal', '05-15', '09-15'),
    (N'Bryce Canyon', N'Sunset Campground', '04-15', '10-15'),
    (N'Crater Lake', N'Mazama', '06-06', '09-30'),
    (N'Crater Lake', N'Lost Creek', '07-01', '10-15'),
    (N'Death Valley', N'Sunset', '10-15', '04-15'),
    (N'Death Valley', N'Texas Springs', '10-15', '05-15'),
    (N'Death Valley', N'Stovepipe Wells', '09-15', '05-15'),
    (N'Denali', N'Savage River', '05-20', '09-15'),
    (N'Denali', N'Sanctuary River', '05-20', '09-15'),
    (N'Denali', N'Teklanika River', '05-20', '09-15'),
    (N'Denali', N'Igloo Creek', '05-20', '09-15'),
    (N'Denali', N'Wonder Lake', '06-08', '09-15'),
    (N'Everglades', N'Long Pine Key', '11-01', '05-31'),
    (N'Glacier', N'Fish Creek', '06-01', '09-07'),
    (N'Glacier', N'Many Glacier', '05-22', '09-20'),
    (N'Glacier', N'St. Mary', '05-22', '09-20'),
    (N'Glacier', N'Two Medicine', '05-22', '09-20'),
    (N'Glacier', N'Rising Sun', '06-15', '09-07'),
    (N'Glacier Bay', N'Bartlett Cove', '05-01', '09-30'),
    (N'Grand Canyon', N'Desert View', '04-15', '10-15'),
    (N'Grand Teton', N'Jenny Lake', '05-08', '09-28'),
    (N'Grand Teton', N'Signal Mountain', '05-08', '10-12'),
    (N'Grand Teton', N'Colter Bay', '05-22', '09-28'),
    (N'Grand Teton', N'Gros Ventre', '04-28', '10-12'),
    (N'Grand Teton', N'Lizard Creek', '06-12', '09-07'),
    (N'Great Basin', N'Upper Lehman Creek', '05-15', '10-31'),
    (N'Great Basin', N'Wheeler Peak', '06-15', '10-15'),
    (N'Great Basin', N'Baker Creek', '05-15', '10-31'),
    (N'Great Basin', N'Grey Cliffs', '05-15', '10-31'),
    (N'Great Sand Dunes', N'Piñon Flats', '04-01', '10-31'),
    (N'Great Smoky Mountains', N'Elkmont', '03-08', '11-30'),
    (N'Great Smoky Mountains', N'Cataloochee', '03-22', '10-31'),
    (N'Great Smoky Mountains', N'Cosby', '04-01', '10-31'),
    (N'Great Smoky Mountains', N'Deep Creek', '04-01', '10-31'),
    (N'Indiana Dunes', N'Dunewood', '04-01', '10-31'),
    (N'Isle Royale', N'Rock Harbor', '04-16', '10-31'),
    (N'Isle Royale', N'Windigo', '04-16', '10-31'),
    (N'Isle Royale', N'Daisy Farm', '04-16', '10-31'),
    (N'Isle Royale', N'Moskey Basin', '04-16', '10-31'),
    (N'Katmai', N'Brooks Camp', '06-01', '09-17'),
    (N'Kenai Fjords', N'Exit Glacier', '05-25', '09-30'),
    (N'Kings Canyon', N'Sunset', '05-22', '09-07'),
    (N'Kings Canyon', N'Crystal Springs', '05-22', '09-15'),
    (N'Kings Canyon', N'Sentinel', '04-24', '11-15'),
    (N'Kings Canyon', N'Moraine', '05-22', '09-15'),
    (N'Kings Canyon', N'Sheep Creek', '05-22', '11-15'),
    (N'Lassen Volcanic', N'Manzanita Lake', '05-22', '10-31'),
    (N'Lassen Volcanic', N'Summit Lake North', '06-15', '09-30'),
    (N'Lassen Volcanic', N'Summit Lake South', '06-15', '09-30'),
    (N'Lassen Volcanic', N'Butte Lake', '06-01', '10-15'),
    (N'Lassen Volcanic', N'Juniper Lake', '07-01', '10-07'),
    (N'Lassen Volcanic', N'Warner Valley', '06-01', '10-07'),
    (N'Mammoth Cave', N'Mammoth Cave Campground', '03-01', '11-30'),
    (N'Mammoth Cave', N'Maple Springs', '03-01', '11-30'),
    (N'Mesa Verde', N'Morefield', '05-01', '10-15'),
    (N'Mount Rainier', N'Cougar Rock', '05-22', '10-11'),
    (N'Mount Rainier', N'Ohanapecosh', '05-22', '10-11'),
    (N'Mount Rainier', N'White River', '06-26', '09-27'),
    (N'Mount Rainier', N'Mowich Lake', '07-10', '10-05'),
    (N'North Cascades', N'Newhalem Creek', '05-22', '09-30'),
    (N'Olympic', N'Sol Duc', '03-20', '10-31'),
    (N'Olympic', N'Fairholme', '04-15', '10-15'),
    (N'Redwood', N'Mill Creek', '05-15', '09-30'),
    (N'Rocky Mountain', N'Glacier Basin', '05-22', '10-12'),
    (N'Rocky Mountain', N'Aspenglen', '05-22', '09-28'),
    (N'Rocky Mountain', N'Timber Creek', '05-22', '10-31'),
    (N'Rocky Mountain', N'Longs Peak', '06-01', '09-15'),
    (N'Sequoia', N'Lodgepole', '05-22', '10-31'),
    (N'Sequoia', N'Dorst Creek', '06-19', '09-07'),
    (N'Sequoia', N'Buckeye Flat', '03-27', '09-30'),
    (N'Sequoia', N'Atwell Mill', '05-22', '10-31'),
    (N'Sequoia', N'Cold Springs', '05-22', '10-31'),
    (N'Shenandoah', N'Mathews Arm', '05-08', '10-31'),
    (N'Shenandoah', N'Big Meadows', '03-27', '11-30'),
    (N'Shenandoah', N'Lewis Mountain', '04-03', '11-08'),
    (N'Shenandoah', N'Loft Mountain', '05-08', '10-31'),
    (N'Yellowstone', N'Madison', '04-24', '10-18'),
    (N'Yellowstone', N'Canyon', '05-29', '09-20'),
    (N'Yellowstone', N'Bridge Bay', '05-15', '09-07'),
    (N'Yellowstone', N'Grant Village', '06-05', '09-13'),
    (N'Yellowstone', N'Norris', '06-05', '09-20'),
    (N'Yellowstone', N'Slough Creek', '06-12', '10-12'),
    (N'Yosemite', N'Lower Pines', '04-10', '10-31'),
    (N'Yosemite', N'North Pines', '04-03', '10-31'),
    (N'Yosemite', N'Tuolumne Meadows', '07-01', '09-25'),
    (N'Zion', N'South Campground', '03-01', '11-30'),
    (N'Zion', N'Lava Point', '05-15', '09-30')
) AS v (ParkName, CampgroundName, SeasonOpens, SeasonCloses)
JOIN dbo.Park AS p ON p.ParkName = v.ParkName
JOIN dbo.Campground AS c ON c.ParkId = p.ParkId AND c.CampgroundName = v.CampgroundName;
