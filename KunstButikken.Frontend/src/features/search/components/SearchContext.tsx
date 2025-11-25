"use client";

import React, { createContext, useContext, useState } from "react";

const SearchContext = createContext<{ query: string; setQuery: (q: string) => void }>({ query: "", setQuery: () => {} });

export function SearchProvider({ children }: { children: React.ReactNode }) {
  const [query, setQuery] = useState("");
  return <SearchContext.Provider value={{ query, setQuery }}>{children}</SearchContext.Provider>;
}

export const useSearch = () => useContext(SearchContext);

