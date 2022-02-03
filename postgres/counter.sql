-- Table: public.counter

CREATE TABLE IF NOT EXISTS public.counter
(
    id integer NOT NULL,
    pollcounter smallint,
    verifycounter smallint,
    CONSTRAINT counter_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.counter
    OWNER to postgres;

CREATE SEQUENCE public.counter_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.counter_id_seq OWNER TO postgres;

ALTER SEQUENCE public.counter_id_seq OWNED BY public.counter.id;

ALTER TABLE ONLY public.counter ALTER COLUMN id SET DEFAULT nextval('public.counter_id_seq'::regclass);

INSERT INTO counter (pollcounter, verifycounter) VALUES(1, 1);