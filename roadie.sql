-- MySQL dump 10.14  Distrib 5.5.60-MariaDB, for Linux (x86_64)
--
-- Host: localhost    Database: roadie
-- ------------------------------------------------------
-- Server version	5.5.60-MariaDB

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `artist`
--

DROP TABLE IF EXISTS `artist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `artist` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `name` varchar(250) NOT NULL,
  `sortName` varchar(250) DEFAULT NULL,
  `rating` smallint(6) NOT NULL,
  `realName` varchar(500) DEFAULT NULL,
  `musicBrainzId` varchar(100) DEFAULT NULL,
  `iTunesId` varchar(100) DEFAULT NULL,
  `amgId` varchar(100) DEFAULT NULL,
  `spotifyId` varchar(100) DEFAULT NULL,
  `thumbnail` blob,
  `profile` text,
  `birthDate` date DEFAULT NULL,
  `beginDate` date DEFAULT NULL,
  `endDate` date DEFAULT NULL,
  `artistType` enum('Person','Group','Orchestra','Choir','Character','Meta','Other') DEFAULT NULL,
  `bioContext` text,
  `bandStatus` enum('Active','On Hold','Split Up','Deceased') DEFAULT NULL,
  `discogsId` varchar(50) DEFAULT NULL,
  `tags` text,
  `alternateNames` text,
  `urls` text,
  `isniList` text,
  `releaseCount` int(11) DEFAULT NULL,
  `trackCount` int(11) DEFAULT NULL,
  `playedCount` int(11) DEFAULT NULL,
  `lastPlayed` datetime DEFAULT NULL,
  `rank` decimal(9,2) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_artist_name` (`name`),
  UNIQUE KEY `ix_artist_sortname` (`sortName`),
  KEY `ix_artist_roadieId` (`roadieId`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `artistAssociation`
--

DROP TABLE IF EXISTS `artistAssociation`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `artistAssociation` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `artistId` int(11) NOT NULL,
  `associatedArtistId` int(11) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `associatedArtistId` (`associatedArtistId`),
  KEY `idx_artistAssociation` (`artistId`,`associatedArtistId`),
  CONSTRAINT `artistAssociation_ibfk_1` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE,
  CONSTRAINT `artistAssociation_ibfk_2` FOREIGN KEY (`associatedArtistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `artistGenreTable`
--

DROP TABLE IF EXISTS `artistGenreTable`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `artistGenreTable` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `artistId` int(11) DEFAULT NULL,
  `genreId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `genreId` (`genreId`),
  KEY `idx_artistGenreAssociation` (`artistId`,`genreId`),
  KEY `ix_artistGenreTable_artistId` (`artistId`),
  CONSTRAINT `artistGenreTable_ibfk_1` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE,
  CONSTRAINT `artistGenreTable_ibfk_2` FOREIGN KEY (`genreId`) REFERENCES `genre` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `bookmark`
--

DROP TABLE IF EXISTS `bookmark`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `bookmark` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) DEFAULT NULL,
  `bookmarkType` smallint(6) DEFAULT NULL,
  `bookmarkTargetId` int(11) DEFAULT NULL,
  `isLocked` tinyint(1) DEFAULT NULL,
  `position` int(11) DEFAULT NULL,
  `comment` varchar(4000) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `bookmark_bookmarkType_IDX` (`bookmarkType`,`bookmarkTargetId`,`userId`) USING BTREE,
  KEY `ix_bookmark_roadieId` (`roadieId`),
  KEY `ix_bookmark_userId` (`userId`),
  CONSTRAINT `bookmark_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `chatMessage`
--

DROP TABLE IF EXISTS `chatMessage`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `chatMessage` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) NOT NULL,
  `message` varchar(5000) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_user` (`userId`),
  CONSTRAINT `chatMessage_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `collection`
--

DROP TABLE IF EXISTS `collection`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `collection` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `name` varchar(100) NOT NULL,
  `sortName` varchar(100) DEFAULT NULL,
  `edition` varchar(200) DEFAULT NULL,
  `listInCSVFormat` varchar(200) DEFAULT NULL,
  `listInCSV` text,
  `description` varchar(4000) DEFAULT NULL,
  `thumbnail` blob,
  `urls` text,
  `maintainerId` int(11) DEFAULT NULL,
  `collectionType` enum('Collection','Chart','Rank','Unknown') DEFAULT NULL,
  `collectionCount` int(11) DEFAULT NULL,
  `tags` text,
  `alternateNames` text,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_collection_name` (`name`),
  KEY `maintainerId` (`maintainerId`),
  KEY `ix_collection_roadieId` (`roadieId`),
  CONSTRAINT `collection_ibfk_1` FOREIGN KEY (`maintainerId`) REFERENCES `user` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `collectionrelease`
--

DROP TABLE IF EXISTS `collectionrelease`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `collectionrelease` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `listNumber` int(11) NOT NULL,
  `releaseId` int(11) DEFAULT NULL,
  `collectionId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `releaseId` (`releaseId`),
  KEY `idx_collection_release` (`collectionId`,`releaseId`),
  KEY `ix_collectionrelease_roadieId` (`roadieId`),
  CONSTRAINT `collectionrelease_ibfk_1` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE,
  CONSTRAINT `collectionrelease_ibfk_2` FOREIGN KEY (`collectionId`) REFERENCES `collection` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `genre`
--

DROP TABLE IF EXISTS `genre`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `genre` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_genre_name` (`name`),
  KEY `ix_genre_roadieId` (`roadieId`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `image`
--

DROP TABLE IF EXISTS `image`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `image` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `image` mediumblob,
  `url` varchar(500) DEFAULT NULL,
  `caption` varchar(100) DEFAULT NULL,
  `signature` varchar(50) DEFAULT NULL,
  `artistId` int(11) DEFAULT NULL,
  `releaseId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_image_releaseId` (`releaseId`),
  KEY `ix_image_roadieId` (`roadieId`),
  KEY `ix_image_artistId` (`artistId`),
  CONSTRAINT `image_ibfk_1` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE,
  CONSTRAINT `image_ibfk_2` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `label`
--

DROP TABLE IF EXISTS `label`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `label` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `name` varchar(250) NOT NULL,
  `sortName` varchar(500) DEFAULT NULL,
  `musicBrainzId` varchar(100) DEFAULT NULL,
  `beginDate` date DEFAULT NULL,
  `endDate` date DEFAULT NULL,
  `imageUrl` varchar(500) DEFAULT NULL,
  `tags` text,
  `alternateNames` text,
  `urls` text,
  `thumbnail` blob,
  `profile` text,
  `discogsId` varchar(50) DEFAULT NULL,
  `artistCount` int(11) DEFAULT NULL,
  `releaseCount` int(11) DEFAULT NULL,
  `trackCount` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_label_name` (`name`),
  KEY `ix_label_roadieId` (`roadieId`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `playlist`
--

DROP TABLE IF EXISTS `playlist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `playlist` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `isPublic` tinyint(1) DEFAULT NULL,
  `name` varchar(100) NOT NULL,
  `description` varchar(1000) DEFAULT NULL,
  `thumbnail` blob,
  `urls` text,
  `tags` text,
  `userId` int(11) DEFAULT NULL,
  `alternateNames` text,
  `trackCount` smallint(6) DEFAULT NULL,
  `duration` int(11) DEFAULT NULL,
  `releaseCount` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_playlist_name` (`name`,`userId`),
  KEY `ix_playlist_roadieId` (`roadieId`),
  KEY `ix_playlist_userId` (`userId`),
  CONSTRAINT `playlist_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `playlisttrack`
--

DROP TABLE IF EXISTS `playlisttrack`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `playlisttrack` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `listNumber` int(11) NOT NULL,
  `trackId` int(11) DEFAULT NULL,
  `playListId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `trackId` (`trackId`),
  KEY `playListId` (`playListId`),
  KEY `ix_playlisttrack_roadieId` (`roadieId`),
  CONSTRAINT `playlisttrack_ibfk_1` FOREIGN KEY (`trackId`) REFERENCES `track` (`id`) ON DELETE CASCADE,
  CONSTRAINT `playlisttrack_ibfk_2` FOREIGN KEY (`playListId`) REFERENCES `playlist` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `release`
--

DROP TABLE IF EXISTS `release`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `release` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `submissionId` int(11) DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `isVirtual` tinyint(1) DEFAULT NULL,
  `title` varchar(250) NOT NULL,
  `alternateNames` text,
  `releaseDate` date DEFAULT NULL,
  `rating` smallint(6) NOT NULL,
  `trackCount` smallint(6) NOT NULL,
  `mediaCount` smallint(6) DEFAULT NULL,
  `thumbnail` blob,
  `profile` text,
  `discogsId` varchar(50) DEFAULT NULL,
  `releaseType` enum('Release','EP','Single','Unknown') DEFAULT NULL,
  `libraryStatus` enum('Complete','Incomplete','Missing','Wishlist') DEFAULT NULL,
  `iTunesId` varchar(100) DEFAULT NULL,
  `amgId` varchar(100) DEFAULT NULL,
  `lastFMId` varchar(100) DEFAULT NULL,
  `lastFMSummary` text,
  `musicBrainzId` varchar(100) DEFAULT NULL,
  `spotifyId` varchar(100) DEFAULT NULL,
  `tags` text,
  `urls` text,
  `artistId` int(11) DEFAULT NULL,
  `lastPlayed` datetime DEFAULT NULL,
  `playedCount` int(11) DEFAULT NULL,
  `duration` int(11) DEFAULT NULL,
  `rank` decimal(9,2) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `idx_releaseArtistAndTitle` (`artistId`,`title`),
  KEY `ix_release_roadieId` (`roadieId`),
  KEY `ix_release_title` (`title`),
  CONSTRAINT `release_ibfk_1` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `releaseGenreTable`
--

DROP TABLE IF EXISTS `releaseGenreTable`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `releaseGenreTable` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `releaseId` int(11) DEFAULT NULL,
  `genreId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `genreId` (`genreId`),
  KEY `idx_releaseGenreTableReleaseAndGenre` (`releaseId`,`genreId`),
  CONSTRAINT `releaseGenreTable_ibfk_1` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE,
  CONSTRAINT `releaseGenreTable_ibfk_2` FOREIGN KEY (`genreId`) REFERENCES `genre` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `releaselabel`
--

DROP TABLE IF EXISTS `releaselabel`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `releaselabel` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `catalogNumber` varchar(200) DEFAULT NULL,
  `beginDate` date DEFAULT NULL,
  `endDate` date DEFAULT NULL,
  `releaseId` int(11) DEFAULT NULL,
  `labelId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `labelId` (`labelId`),
  KEY `idx_release_label` (`releaseId`,`labelId`),
  KEY `ix_releaselabel_roadieId` (`roadieId`),
  CONSTRAINT `releaselabel_ibfk_1` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE,
  CONSTRAINT `releaselabel_ibfk_2` FOREIGN KEY (`labelId`) REFERENCES `label` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `releasemedia`
--

DROP TABLE IF EXISTS `releasemedia`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `releasemedia` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `releaseMediaNumber` smallint(6) DEFAULT NULL,
  `releaseSubTitle` varchar(500) DEFAULT NULL,
  `trackCount` smallint(6) NOT NULL,
  `releaseId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_releasemedia_roadieId` (`roadieId`),
  KEY `releasemedia_releaseId_IDX` (`releaseId`,`releaseMediaNumber`) USING BTREE,
  CONSTRAINT `releasemedia_ibfk_1` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `request`
--

DROP TABLE IF EXISTS `request`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `request` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) DEFAULT NULL,
  `description` varchar(500) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_request_roadieId` (`roadieId`),
  KEY `requestartist_ibfk_1` (`userId`),
  CONSTRAINT `requestartist_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `scanHistory`
--

DROP TABLE IF EXISTS `scanHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `scanHistory` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) NOT NULL,
  `forArtistId` int(11) DEFAULT NULL,
  `forReleaseId` int(11) DEFAULT NULL,
  `newArtists` int(11) DEFAULT NULL,
  `newReleases` int(11) DEFAULT NULL,
  `newTracks` int(11) DEFAULT NULL,
  `timeSpanInSeconds` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_scanHistory_roadieId` (`roadieId`),
  KEY `rscanHistoryt_ibfk_1` (`userId`),
  CONSTRAINT `scanHistory_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `submission`
--

DROP TABLE IF EXISTS `submission`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `submission` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) DEFAULT NULL,
  `IsLocked` bit(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_submission_roadieId` (`roadieId`),
  KEY `submission_ibfk_1` (`userId`),
  CONSTRAINT `submission_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `track`
--

DROP TABLE IF EXISTS `track`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `track` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `filePath` varchar(1000) DEFAULT NULL,
  `fileName` varchar(500) DEFAULT NULL,
  `fileSize` int(11) DEFAULT NULL,
  `hash` varchar(32) DEFAULT NULL,
  `playedCount` int(11) DEFAULT NULL,
  `lastPlayed` datetime DEFAULT NULL,
  `partTitles` text,
  `rating` smallint(6) NOT NULL,
  `musicBrainzId` varchar(100) DEFAULT NULL,
  `lastFMId` varchar(50) DEFAULT NULL,
  `amgId` varchar(100) DEFAULT NULL,
  `spotifyId` varchar(100) DEFAULT NULL,
  `title` varchar(250) NOT NULL,
  `alternateNames` text,
  `trackNumber` smallint(6) NOT NULL,
  `duration` int(11) DEFAULT NULL,
  `tags` text,
  `releaseMediaId` int(11) DEFAULT NULL,
  `artistId` int(11) DEFAULT NULL,
  `isrc` varchar(15) DEFAULT NULL,
  `thumbnail` blob,
  PRIMARY KEY (`id`),
  UNIQUE KEY `idx_track_unique_to_eleasemedia` (`releaseMediaId`,`trackNumber`),
  UNIQUE KEY `track_hash_IDX` (`hash`) USING BTREE,
  KEY `ix_track_title` (`title`),
  KEY `ix_track_roadieId` (`roadieId`),
  KEY `track_artistId_IDX` (`artistId`) USING BTREE,
  KEY `track_releaseMediaId_IDX` (`releaseMediaId`) USING BTREE,
  CONSTRAINT `track_ibfk_1` FOREIGN KEY (`releaseMediaId`) REFERENCES `releasemedia` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `trackPlaylistTrack`
--

DROP TABLE IF EXISTS `trackPlaylistTrack`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `trackPlaylistTrack` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `trackId` int(11) DEFAULT NULL,
  `playlisttrackId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_trackPlaylistTrack_trackId` (`trackId`),
  KEY `ix_trackPlaylistTrack_playlisttrackId` (`playlisttrackId`),
  CONSTRAINT `trackPlaylistTrack_ibfk_1` FOREIGN KEY (`trackId`) REFERENCES `track` (`id`),
  CONSTRAINT `trackPlaylistTrack_ibfk_2` FOREIGN KEY (`playlisttrackId`) REFERENCES `playlisttrack` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `lastApiAccess` datetime DEFAULT NULL,
  `username` varchar(20) NOT NULL,
  `password` varchar(100) NOT NULL,
  `apiToken` varchar(100) DEFAULT NULL,
  `email` varchar(100) NOT NULL,
  `registeredOn` datetime DEFAULT NULL,
  `lastLogin` datetime DEFAULT NULL,
  `isActive` tinyint(1) DEFAULT NULL,
  `avatar` blob,
  `doUseHtmlPlayer` tinyint(1) DEFAULT NULL,
  `timezone` varchar(50) DEFAULT NULL,
  `playerTrackLimit` smallint(6) DEFAULT '50',
  `profile` text,
  `timeformat` varchar(50) DEFAULT 'YYYY-MM-DD HH:mm:ss',
  `isPrivate` tinyint(1) DEFAULT NULL,
  `recentlyPlayedLimit` smallint(6) DEFAULT '50',
  `randomReleaseLimit` smallint(6) DEFAULT '12',
  `ftpUrl` varchar(250) DEFAULT NULL,
  `ftpDirectory` varchar(500) DEFAULT NULL,
  `ftpUsername` varchar(50) DEFAULT NULL,
  `ftpPassword` varchar(500) DEFAULT NULL,
  `AccessFailedCount` mediumint(9) DEFAULT NULL,
  `ConcurrencyStamp` varchar(100) DEFAULT NULL,
  `SecurityStamp` varchar(100) DEFAULT NULL,
  `EmailConfirmed` bit(1) DEFAULT NULL,
  `LockoutEnabled` bit(1) DEFAULT NULL,
  `LockoutEnd` timestamp NULL DEFAULT NULL,
  `TwoFactorEnabled` bit(1) DEFAULT NULL,
  `NormalizedEmail` varchar(100) DEFAULT NULL,
  `NormalizedUserName` varchar(20) DEFAULT NULL,
  `PhoneNumber` varchar(100) DEFAULT NULL,
  `PhoneNumberConfirmed` bit(1) DEFAULT NULL,
  `removeTrackFromQueAfterPlayed` bit(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`),
  UNIQUE KEY `ix_user_username` (`username`),
  KEY `ix_user_roadieId` (`roadieId`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `userClaims`
--

DROP TABLE IF EXISTS `userClaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userClaims` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `userId` int(11) NOT NULL,
  `claimType` varchar(200) NOT NULL,
  `claimValue` varchar(200) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_userClaims_userId` (`userId`),
  CONSTRAINT `userClaims_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `userQue`
--

DROP TABLE IF EXISTS `userQue`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userQue` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) NOT NULL,
  `trackId` int(11) NOT NULL,
  `position` int(11) DEFAULT NULL,
  `queSortOrder` smallint(6) NOT NULL,
  `isCurrent` bit(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_user` (`userId`),
  KEY `userQue_ibfk_2` (`trackId`),
  CONSTRAINT `userQue_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `userQue_ibfk_2` FOREIGN KEY (`trackId`) REFERENCES `track` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `userRoleClaims`
--

DROP TABLE IF EXISTS `userRoleClaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userRoleClaims` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `userRoleId` int(11) NOT NULL,
  `claimType` varchar(200) NOT NULL,
  `claimValue` varchar(200) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_userRoleClaims_userRoleId` (`userRoleId`),
  CONSTRAINT `userRoleClaims_ibfk_1` FOREIGN KEY (`userRoleId`) REFERENCES `userrole` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `userartist`
--

DROP TABLE IF EXISTS `userartist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userartist` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `isFavorite` tinyint(1) DEFAULT NULL,
  `isDisliked` tinyint(1) DEFAULT NULL,
  `rating` smallint(6) NOT NULL,
  `userId` int(11) DEFAULT NULL,
  `artistId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `userartist_userId_IDX` (`userId`,`artistId`) USING BTREE,
  KEY `artistId` (`artistId`),
  KEY `ix_userartist_roadieId` (`roadieId`),
  CONSTRAINT `userartist_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `userartist_ibfk_2` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `userrelease`
--

DROP TABLE IF EXISTS `userrelease`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userrelease` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `isFavorite` tinyint(1) DEFAULT NULL,
  `isDisliked` tinyint(1) DEFAULT NULL,
  `rating` smallint(6) NOT NULL,
  `userId` int(11) DEFAULT NULL,
  `releaseId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `userrelease_userId_IDX` (`userId`,`releaseId`) USING BTREE,
  KEY `releaseId` (`releaseId`),
  KEY `ix_userrelease_roadieId` (`roadieId`),
  CONSTRAINT `userrelease_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `userrelease_ibfk_2` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `userrole`
--

DROP TABLE IF EXISTS `userrole`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `userrole` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `name` varchar(80) NOT NULL,
  `description` varchar(200) DEFAULT NULL,
  `ConcurrencyStamp` varchar(100) DEFAULT NULL,
  `NormalizedName` varchar(80) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`),
  KEY `ix_userrole_roadieId` (`roadieId`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usersInRoles`
--

DROP TABLE IF EXISTS `usersInRoles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `usersInRoles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `userId` int(11) DEFAULT NULL,
  `userRoleId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `userRoleId` (`userRoleId`),
  KEY `ix_usersInRoles_userId` (`userId`),
  CONSTRAINT `usersInRoles_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `usersInRoles_ibfk_2` FOREIGN KEY (`userRoleId`) REFERENCES `userrole` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usertrack`
--

DROP TABLE IF EXISTS `usertrack`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `usertrack` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `isFavorite` tinyint(1) DEFAULT NULL,
  `isDisliked` tinyint(1) DEFAULT NULL,
  `rating` smallint(6) NOT NULL,
  `playedCount` int(11) DEFAULT NULL,
  `lastPlayed` datetime DEFAULT NULL,
  `userId` int(11) DEFAULT NULL,
  `trackId` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `usertrack_userId_IDX` (`userId`,`trackId`) USING BTREE,
  KEY `trackId` (`trackId`),
  KEY `ix_usertrack_roadieId` (`roadieId`),
  CONSTRAINT `usertrack_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `usertrack_ibfk_2` FOREIGN KEY (`trackId`) REFERENCES `track` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Temporary table structure for view `vTrackList`
--

DROP TABLE IF EXISTS `vTrackList`;
/*!50001 DROP VIEW IF EXISTS `vTrackList`*/;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `vTrackList` (
  `trackId` tinyint NOT NULL,
  `trackRoadieId` tinyint NOT NULL,
  `trackCreatedDate` tinyint NOT NULL,
  `trackLastUpdated` tinyint NOT NULL,
  `trackDuration` tinyint NOT NULL,
  `trackFileSize` tinyint NOT NULL,
  `trackPlayedCount` tinyint NOT NULL,
  `trackRating` tinyint NOT NULL,
  `trackTitle` tinyint NOT NULL,
  `releaseMediaNumber` tinyint NOT NULL,
  `releaseId` tinyint NOT NULL,
  `releaseRoadieId` tinyint NOT NULL,
  `releaseTitle` tinyint NOT NULL,
  `releaseCreatedDate` tinyint NOT NULL,
  `releaseLastUpdated` tinyint NOT NULL,
  `releaseLibraryStatus` tinyint NOT NULL,
  `releaseRating` tinyint NOT NULL,
  `releaseDate` tinyint NOT NULL,
  `releaseStatus` tinyint NOT NULL,
  `releaseTrackCount` tinyint NOT NULL,
  `releasePlayedCount` tinyint NOT NULL,
  `artistId` tinyint NOT NULL,
  `artistRoadieId` tinyint NOT NULL,
  `artistName` tinyint NOT NULL,
  `artistRating` tinyint NOT NULL,
  `artistCreatedDate` tinyint NOT NULL,
  `artistLastUpdated` tinyint NOT NULL,
  `artistLastPlayed` tinyint NOT NULL,
  `artistPlayedCount` tinyint NOT NULL,
  `artistReleaseCount` tinyint NOT NULL,
  `artistTrackCount` tinyint NOT NULL,
  `artistSortName` tinyint NOT NULL,
  `trackArtistId` tinyint NOT NULL,
  `trackArtistRoadieId` tinyint NOT NULL,
  `trackArtistName` tinyint NOT NULL,
  `trackArtistRating` tinyint NOT NULL,
  `trackArtistCreatedDate` tinyint NOT NULL,
  `trackArtistLastUpdated` tinyint NOT NULL,
  `trackArtistLastPlayed` tinyint NOT NULL,
  `trackArtistPlayedCount` tinyint NOT NULL,
  `trackArtistReleaseCount` tinyint NOT NULL,
  `trackArtistTrackCount` tinyint NOT NULL,
  `trackArtistSortName` tinyint NOT NULL
) ENGINE=MyISAM */;
SET character_set_client = @saved_cs_client;

--
-- Final view structure for view `vTrackList`
--

/*!50001 DROP TABLE IF EXISTS `vTrackList`*/;
/*!50001 DROP VIEW IF EXISTS `vTrackList`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`roadie`@`%` SQL SECURITY DEFINER */
/*!50001 VIEW `vTrackList` AS select `t`.`id` AS `trackId`,`t`.`roadieId` AS `trackRoadieId`,`t`.`createdDate` AS `trackCreatedDate`,`t`.`lastUpdated` AS `trackLastUpdated`,`t`.`duration` AS `trackDuration`,`t`.`fileSize` AS `trackFileSize`,`t`.`playedCount` AS `trackPlayedCount`,`t`.`rating` AS `trackRating`,`t`.`title` AS `trackTitle`,`rm`.`releaseMediaNumber` AS `releaseMediaNumber`,`r`.`id` AS `releaseId`,`r`.`roadieId` AS `releaseRoadieId`,`r`.`title` AS `releaseTitle`,`r`.`createdDate` AS `releaseCreatedDate`,`r`.`lastUpdated` AS `releaseLastUpdated`,`r`.`libraryStatus` AS `releaseLibraryStatus`,`r`.`rating` AS `releaseRating`,`r`.`releaseDate` AS `releaseDate`,`r`.`status` AS `releaseStatus`,`r`.`trackCount` AS `releaseTrackCount`,`r`.`playedCount` AS `releasePlayedCount`,`ra`.`id` AS `artistId`,`ra`.`roadieId` AS `artistRoadieId`,`ra`.`name` AS `artistName`,`ra`.`rating` AS `artistRating`,`ra`.`createdDate` AS `artistCreatedDate`,`ra`.`lastUpdated` AS `artistLastUpdated`,`ra`.`lastPlayed` AS `artistLastPlayed`,`ra`.`playedCount` AS `artistPlayedCount`,`ra`.`releaseCount` AS `artistReleaseCount`,`ra`.`trackCount` AS `artistTrackCount`,`ra`.`sortName` AS `artistSortName`,`ta`.`id` AS `trackArtistId`,`ta`.`roadieId` AS `trackArtistRoadieId`,`ta`.`name` AS `trackArtistName`,`ta`.`rating` AS `trackArtistRating`,`ta`.`createdDate` AS `trackArtistCreatedDate`,`ta`.`lastUpdated` AS `trackArtistLastUpdated`,`ta`.`lastPlayed` AS `trackArtistLastPlayed`,`ta`.`playedCount` AS `trackArtistPlayedCount`,`ta`.`releaseCount` AS `trackArtistReleaseCount`,`ta`.`trackCount` AS `trackArtistTrackCount`,`ta`.`sortName` AS `trackArtistSortName` from ((((`track` `t` join `releasemedia` `rm` on((`t`.`releaseMediaId` = `rm`.`id`))) join `release` `r` on((`rm`.`releaseId` = `r`.`id`))) join `artist` `ra` on((`r`.`artistId` = `ra`.`id`))) left join `artist` `ta` on((`t`.`artistId` = `ta`.`id`))) where (`t`.`hash` is not null) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2019-03-02 15:11:06
