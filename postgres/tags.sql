-- Table: public.tags
--This table is to be used for the tags feature.

CREATE TABLE IF NOT EXISTS public.tags
(
    id integer NOT NULL,
    tag_name character varying(50) COLLATE pg_catalog."default",
    tag_content character varying(3000) COLLATE pg_catalog."default",
    creator_user_id bigint,
    updater_user_id bigint,
    created_timestamp bigint,
    last_updated_timestamp bigint,
    version_count integer,
    CONSTRAINT tags_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.tags
    OWNER to postgres;

CREATE SEQUENCE public.tags_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE public.tags_id_seq OWNER TO postgres;

ALTER SEQUENCE public.tags_id_seq OWNED BY public.tags.id;

ALTER TABLE ONLY public.tags ALTER COLUMN id SET DEFAULT nextval('public.tags_id_seq'::regclass);