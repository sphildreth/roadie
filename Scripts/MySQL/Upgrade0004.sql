-- Add new column to Genre for Normalized Name < 1.0.2.1 database
ALTER TABLE `genre` ADD `normalizedName` varchar(100) NULL;
CREATE INDEX `genre_normalizedName_IDX` USING BTREE ON `genre` (normalizedName);