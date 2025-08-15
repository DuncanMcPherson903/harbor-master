-- Drop tables if they exist
DROP TABLE IF EXISTS ships;
DROP TABLE IF EXISTS docks;
DROP TABLE IF EXISTS haulers;

-- Create tables
CREATE TABLE docks (
    id SERIAL PRIMARY KEY,
    location VARCHAR(100) NOT NULL,
    capacity INTEGER NOT NULL
);

CREATE TABLE haulers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    capacity INTEGER NOT NULL
);

CREATE TABLE ships (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(100) NOT NULL,
    dock_id INTEGER,
    hauler_id INTEGER,
    FOREIGN KEY (dock_id) REFERENCES docks(id) ON DELETE SET NULL,
    FOREIGN KEY (hauler_id) REFERENCES haulers(id) ON DELETE SET NULL
);
