-- Create collection missing table for < 1.0.2.0 database
CREATE TABLE `collectionMissing` (
  `id` int(11) NOT NULL AUTO_INCREMENT, 
  `collectionId` int(11) NOT NULL,
  `isArtistFound` tinyint(1) DEFAULT NULL,
  `position` int(11) NOT NULL,
  `artist` varchar(1000) COLLATE utf8mb4_unicode_ci NOT NULL,
  `release` varchar(1000) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`),  
  KEY `ix_collection_collectionId` (`collectionId`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create artistSimilar table for < 1.0.2.0 database
CREATE TABLE `artistSimilar` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `artistId` int(11) NOT NULL,
  `similarArtistId` int(11) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `similarArtistId` (`similarArtistId`),
  KEY `idx_artistSimilar` (`artistId`,`similarArtistId`),
  CONSTRAINT `artistSimilar_ibfk_1` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE,
  CONSTRAINT `artistSimilar_ibfk_2` FOREIGN KEY (`similarArtistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
