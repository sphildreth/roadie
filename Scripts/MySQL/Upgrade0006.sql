-- v1.0.3.1 -- Genre tables modifications for https://github.com/sphildreth/roadie-vuejs/issues/63
ALTER TABLE `genre` ADD `thumbnail` BLOB NULL;
ALTER TABLE `genre` ADD `alternateNames` MEDIUMTEXT NULL;
ALTER TABLE `genre` ADD `description` varchar(4000) NULL;
ALTER TABLE `genre` ADD `tags` MEDIUMTEXT NULL;
ALTER TABLE `user` ADD `defaultRowsPerPage` SMALLINT NULL;